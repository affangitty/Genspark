using BusBooking.Application.DTOs.Booking;
using BusBooking.Application.Features.Bookings.Commands;
using BusBooking.Application.Interfaces;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using BusBooking.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BusBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISeatLockService _seatLockService;
    private readonly ITicketService _ticketService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMailService _mailService;

    public BookingController(
        IMediator mediator,
        ISeatLockService seatLockService,
        ITicketService ticketService,
        IUnitOfWork unitOfWork,
        IMailService mailService)
    {
        _mediator = mediator;
        _seatLockService = seatLockService;
        _ticketService = ticketService;
        _unitOfWork = unitOfWork;
        _mailService = mailService;
    }

    /// <summary>
    /// Create a new booking with multiple passengers
    /// Handles seat locking, payment processing, and ticket generation
    /// </summary>
    [HttpPost("create")]
    public async Task<ActionResult<BookingResponseDto>> CreateBooking(
        [FromBody] CreateBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            var command = new CreateBookingCommand
            {
                BusId = request.BusId,
                UserId = userId,
                JourneyDate = request.JourneyDate,
                Passengers = request.Passengers
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating the booking", error = ex.Message });
        }
    }

    /// <summary>
    /// Lock a seat for the current user (prevent double booking)
    /// Returns true if lock was successful
    /// </summary>
    [HttpPost("lock-seat")]
    public async Task<ActionResult<bool>> LockSeat([FromBody] SeatLockRequestDto request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();
            var cfg = await _unitOfWork.PlatformConfig.GetCurrentAsync();
            var lockSeconds = Math.Clamp((cfg?.SeatLockDurationMinutes ?? 10) * 60, 60, 7200);
            var success = await _seatLockService.LockSeatAsync(request.SeatId, userId, lockSeconds, request.JourneyDate.Date);

            if (!success)
                return BadRequest(new { message = "Seat is already locked or unavailable" });

            return Ok(true);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error locking seat", error = ex.Message });
        }
    }

    /// <summary>
    /// Unlock a seat (user deselects it)
    /// </summary>
    [HttpPost("unlock-seat")]
    public async Task<ActionResult<bool>> UnlockSeat([FromBody] SeatLockRequestDto request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();
            var success = await _seatLockService.UnlockSeatAsync(request.SeatId, userId, request.JourneyDate.Date);

            if (!success)
                return BadRequest(new { message = "Seat is not locked by you" });

            return Ok(true);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error unlocking seat", error = ex.Message });
        }
    }

    /// <summary>
    /// Extend seat lock duration (user still in checkout)
    /// </summary>
    [HttpPost("extend-lock")]
    public async Task<ActionResult<bool>> ExtendLock(
        [FromQuery] Guid seatId,
        [FromQuery] DateTime journeyDate,
        [FromQuery] int additionalSeconds = 300)
    {
        try
        {
            var success = await _seatLockService.ExtendLockAsync(seatId, additionalSeconds, journeyDate.Date);

            if (!success)
                return BadRequest(new { message = "Could not extend seat lock" });

            return Ok(true);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error extending lock", error = ex.Message });
        }
    }

    /// <summary>
    /// Get booked seats for a bus on a specific date
    /// Used to display seat availability
    /// </summary>
    [HttpGet("bus/{busId}/booked-seats")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Guid>>> GetBookedSeats(
        Guid busId,
        [FromQuery] DateTime journeyDate)
    {
        var bookedSeats = await _unitOfWork.Bookings.GetBookedSeatIdsByBusAndDateAsync(busId, journeyDate.Date);
        return Ok(bookedSeats);
    }

    /// <summary>
    /// Download ticket PDF for a confirmed booking
    /// </summary>
    [HttpGet("{bookingId}/ticket")]
    public async Task<IActionResult> DownloadTicket(Guid bookingId)
    {
        try
        {
            var ticketBytes = await _ticketService.GenerateTicketAsync(bookingId);

            if (ticketBytes.Length == 0)
                return NotFound(new { message = "Booking not found" });

            return File(ticketBytes, "application/pdf", $"Ticket_{bookingId}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error generating ticket", error = ex.Message });
        }
    }

    /// <summary>
    /// Cancel a booking and process refund
    /// </summary>
    [HttpPost("{bookingId}/cancel")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<CancellationResponseDto>> CancelBooking(
        Guid bookingId,
        [FromBody] CancelBookingRequestDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);

        if (booking == null)
            return NotFound(new { message = "Booking not found" });

        if (booking.UserId != userId)
            return Forbid();

        if (booking.Status != BookingStatus.Confirmed)
            return BadRequest(new { message = "Only confirmed bookings can be cancelled" });

        if (booking.Cancellation != null)
            return BadRequest(new { message = "Booking is already cancelled" });

        var now = DateTime.UtcNow;
        var hoursToJourney = (booking.JourneyDate - now).TotalHours;
        decimal refundPercentage = hoursToJourney >= 24 ? 100m : hoursToJourney >= 12 ? 50m : 0m;
        decimal refundAmount = Math.Round((booking.TotalAmount * refundPercentage) / 100m, 2);

        booking.Status = BookingStatus.Cancelled;
        _unitOfWork.Bookings.Update(booking);

        var cancellation = new Cancellation
        {
            BookingId = booking.Id,
            CancelledAt = now,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Cancelled by user" : request.Reason,
            RefundPercentage = refundPercentage,
            RefundAmount = refundAmount,
            IsAdminInitiated = false
        };
        await _unitOfWork.Bookings.AddCancellationAsync(cancellation);

        if (booking.Payment != null)
        {
            booking.Payment.RefundAmount = refundAmount;
            booking.Payment.RefundedAt = now;
            if (refundAmount <= 0)
                booking.Payment.Status = PaymentStatus.Success;
            else if (refundAmount < booking.Payment.Amount)
                booking.Payment.Status = PaymentStatus.PartialRefund;
            else
                booking.Payment.Status = PaymentStatus.Refunded;
        }

        var passengerSeatIds = booking.Passengers.Select(p => p.SeatId).ToList();
        await _unitOfWork.SeatLocks.ReleaseLocksForSeatsAsync(passengerSeatIds, booking.JourneyDate.Date);
        await _unitOfWork.SaveChangesAsync();

        await _mailService.SendCancellationNoticeAsync(booking.User.Email, booking.BookingReference, refundAmount);

        return Ok(new CancellationResponseDto
        {
            BookingId = booking.Id,
            BookingReference = booking.BookingReference,
            RefundPercentage = refundPercentage,
            RefundAmount = refundAmount,
            CancelledAt = now
        });
    }

    [HttpGet("history/upcoming")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<IEnumerable<BookingResponseDto>>> GetUpcomingBookings()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();
        var bookings = await _unitOfWork.Bookings.GetUpcomingByUserIdAsync(userId, DateTime.UtcNow);
        return Ok(bookings.Select(MapBookingResponse));
    }

    [HttpGet("history/past")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<IEnumerable<BookingResponseDto>>> GetPastBookings()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();
        var bookings = await _unitOfWork.Bookings.GetPastByUserIdAsync(userId, DateTime.UtcNow);
        return Ok(bookings.Select(MapBookingResponse));
    }

    [HttpGet("history/cancelled")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<IEnumerable<BookingResponseDto>>> GetCancelledBookings()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();
        var bookings = await _unitOfWork.Bookings.GetCancelledByUserIdAsync(userId);
        return Ok(bookings.Select(MapBookingResponse));
    }

    [HttpGet("operator")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetOperatorBookings()
    {
        var operatorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (operatorIdClaim == null || !Guid.TryParse(operatorIdClaim.Value, out var operatorId))
            return Unauthorized();

        var bookings = await _unitOfWork.Bookings.GetByOperatorIdAsync(operatorId);
        var bookingDtos = bookings.Select(b => new OperatorBookingDto
        {
            BookingId = b.Id,
            BookingReference = b.BookingReference,
            JourneyDate = b.JourneyDate,
            UserName = b.User.FullName,
            UserEmail = b.User.Email,
            BusNumber = b.Bus.BusNumber,
            Route = $"{b.Bus.Route?.SourceCity} -> {b.Bus.Route?.DestinationCity}",
            TotalAmount = b.TotalAmount,
            RefundAmount = b.Cancellation?.RefundAmount ?? 0m,
            Status = b.Status.ToString(),
            Passengers = b.Passengers.Select(p => new ManifestPassengerDto
            {
                PassengerName = p.PassengerName,
                Age = p.Age,
                Gender = p.Gender,
                SeatNumber = p.SeatNumber,
                BookingReference = b.BookingReference
            }).ToList()
        }).ToList();

        var summary = new OperatorBookingSummaryDto
        {
            TotalBookings = bookingDtos.Count,
            ConfirmedBookings = bookingDtos.Count(b => b.Status == BookingStatus.Confirmed.ToString()),
            CancelledBookings = bookingDtos.Count(b => b.Status == BookingStatus.Cancelled.ToString() || b.Status == BookingStatus.CancelledByAdmin.ToString()),
            GrossRevenue = bookingDtos.Where(b => b.Status == BookingStatus.Confirmed.ToString()).Sum(b => b.TotalAmount),
            TotalRefunds = bookingDtos.Sum(b => b.RefundAmount)
        };
        summary.NetRevenue = summary.GrossRevenue - summary.TotalRefunds;

        return Ok(new { summary, bookings = bookingDtos });
    }

    private static BookingResponseDto MapBookingResponse(Booking booking)
    {
        return new BookingResponseDto
        {
            Id = booking.Id,
            BookingReference = booking.BookingReference,
            BusNumber = booking.Bus.BusNumber,
            BusName = booking.Bus.BusName,
            SourceCity = booking.Bus.Route?.SourceCity ?? string.Empty,
            DestinationCity = booking.Bus.Route?.DestinationCity ?? string.Empty,
            JourneyDate = booking.JourneyDate,
            BoardingAddress = booking.BoardingAddress,
            DropOffAddress = booking.DropOffAddress,
            BaseFareTotal = booking.BaseFareTotal,
            ConvenienceFee = booking.ConvenienceFee,
            TotalAmount = booking.TotalAmount,
            Status = booking.Status.ToString(),
            CreatedAt = booking.CreatedAt,
            Passengers = booking.Passengers.Select(p => new PassengerResponseDto
            {
                PassengerName = p.PassengerName,
                Age = p.Age,
                Gender = p.Gender,
                SeatNumber = p.SeatNumber
            }).ToList()
        };
    }
}

