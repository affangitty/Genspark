using BusBooking.Domain.Common;

namespace BusBooking.Domain.Entities;

/// <summary>
/// One record per passenger per booking.
/// Each passenger is linked to a specific seat.
/// </summary>
public class BookingPassenger : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid SeatId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;  // "Male" | "Female" | "Other"
    public string SeatNumber { get; set; } = string.Empty;  // Denormalized for ticket display

    // Navigation
    public Booking Booking { get; set; } = null!;
    public Seat Seat { get; set; } = null!;
}