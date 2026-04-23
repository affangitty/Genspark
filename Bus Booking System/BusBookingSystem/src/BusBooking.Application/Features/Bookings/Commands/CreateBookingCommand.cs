using BusBooking.Application.DTOs.Booking;
using MediatR;

namespace BusBooking.Application.Features.Bookings.Commands;

/// <summary>
/// Command to create a new booking with multiple passengers and locked seats
/// </summary>
public class CreateBookingCommand : IRequest<BookingResponseDto>
{
    public Guid BusId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JourneyDate { get; set; }
    public List<PassengerDto> Passengers { get; set; } = new();
}
