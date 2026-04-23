using BusBooking.Application.DTOs.Booking;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using BusBooking.Domain.Interfaces;
using MediatR;
using AutoMapper;
using BusBooking.Application.Interfaces;

namespace BusBooking.Application.Features.Bookings.Commands;

/// <summary>
/// Handler for creating bookings with multiple passengers, seat locking, and payment processing
/// </summary>
public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPaymentService _paymentService;
    private readonly ITicketService _ticketService;
    private readonly IMailService _mailService;

    public CreateBookingCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPaymentService paymentService,
        ITicketService ticketService,
        IMailService mailService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _paymentService = paymentService;
        _ticketService = ticketService;
        _mailService = mailService;
    }

    public async Task<BookingResponseDto> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        // Validate bus and journey date
        var bus = await _unitOfWork.Buses.GetByIdAsync(request.BusId);
        if (bus == null)
            throw new ArgumentException("Bus not found");

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
        if (user == null)
            throw new ArgumentException("User not found");

        // Check if all requested seats are still available
        var bookedSeats = await _unitOfWork.Bookings.GetBookedSeatIdsByBusAndDateAsync(
            request.BusId, request.JourneyDate);
        var lockedSeats = await _unitOfWork.SeatLocks.GetActiveLockSeatIdsByBusAndDateAsync(
            request.BusId, request.JourneyDate, exceptUserId: request.UserId);
        var unavailableSeats = new HashSet<Guid>(bookedSeats.Concat(lockedSeats));

        foreach (var passenger in request.Passengers)
        {
            if (unavailableSeats.Contains(passenger.SeatId))
                throw new InvalidOperationException($"Seat is not available for passenger {passenger.PassengerName}");
        }

        // Calculate fares
        var platformConfig = await _unitOfWork.PlatformConfig.GetCurrentAsync();
        var convenienceFeePercentage = platformConfig?.ConvenienceFeePercentage ?? 5m;
        var baseFareTotal = bus.BaseFare * request.Passengers.Count;
        var convenienceFee = (baseFareTotal * convenienceFeePercentage) / 100;
        var totalAmount = baseFareTotal + convenienceFee;

        // Get boarding and drop-off addresses from operator locations
        var operatorLocations = await _unitOfWork.BusOperators.GetLocationsByOperatorIdAsync(bus.OperatorId);
        var boardingAddress = operatorLocations.FirstOrDefault()?.AddressLine ?? "TBD";
        var dropOffAddress = operatorLocations.LastOrDefault()?.AddressLine ?? "TBD";

        // Create booking
        var booking = new Booking
        {
            BookingReference = GenerateBookingReference(),
            UserId = request.UserId,
            BusId = request.BusId,
            JourneyDate = request.JourneyDate,
            Status = BookingStatus.Pending,
            BaseFareTotal = baseFareTotal,
            ConvenienceFee = convenienceFee,
            TotalAmount = totalAmount,
            BoardingAddress = boardingAddress,
            DropOffAddress = dropOffAddress
        };

        await _unitOfWork.Bookings.AddAsync(booking);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Add passengers
        foreach (var passengerDto in request.Passengers)
        {
            var seat = await _unitOfWork.Seats.GetByIdAsync(passengerDto.SeatId);
            if (seat == null)
                throw new ArgumentException("Seat not found");

            var passenger = new BookingPassenger
            {
                BookingId = booking.Id,
                SeatId = passengerDto.SeatId,
                PassengerName = passengerDto.PassengerName,
                Age = passengerDto.Age,
                Gender = passengerDto.Gender,
                SeatNumber = seat.SeatNumber
            };

            // Initialize context to allow adding without explicit repository method
            await _unitOfWork.Bookings.AddPassengerAsync(passenger);
        }

        // Create payment record
        var payment = new Payment
        {
            BookingId = booking.Id,
            Amount = totalAmount,
            Status = PaymentStatus.Pending,
            TransactionId = Guid.NewGuid().ToString()
        };
        await _unitOfWork.Bookings.AddPaymentAsync(payment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Process payment
        var (paymentSuccess, transactionId) = await _paymentService.ProcessPaymentAsync(booking.Id, totalAmount);

        if (!paymentSuccess)
        {
            // Mark booking as cancelled and payment as failed
            booking.Status = BookingStatus.Cancelled;
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = "Payment gateway declined";
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new InvalidOperationException("Payment failed. Please try again.");
        }

        // Update payment with actual transaction ID
        payment.Status = PaymentStatus.Success;
        payment.PaidAt = DateTime.UtcNow;
        payment.TransactionId = transactionId;

        // Mark booking as confirmed
        booking.Status = BookingStatus.Confirmed;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Generate ticket
        var ticketPath = await _ticketService.GenerateTicketFilePathAsync(booking.Id);

        // Send confirmation email
        await _mailService.SendBookingConfirmationAsync(user.Email, booking.BookingReference, ticketPath);

        // Release seat locks after successful booking
        await ReleaseSeatLocks(request.JourneyDate, request.Passengers, cancellationToken);

        // Retrieve updated booking for response
        var finalBooking = await _unitOfWork.Bookings.GetByIdAsync(booking.Id);
        var response = _mapper.Map<BookingResponseDto>(finalBooking);
        return response;
    }

    private string GenerateBookingReference()
    {
        // Format: BB-YYYYMMDD-XXXX (e.g., BB-20240501-A1B2)
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomSuffix = Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
        return $"BB-{timestamp}-{randomSuffix}";
    }

    private async Task ReleaseSeatLocks(DateTime journeyDate, IEnumerable<PassengerDto> passengers, CancellationToken cancellationToken)
    {
        var seatIds = passengers.Select(p => p.SeatId).ToList();
        await _unitOfWork.SeatLocks.ReleaseLocksForSeatsAsync(seatIds, journeyDate.Date);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
