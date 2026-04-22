using BusBooking.Domain.Common;
using BusBooking.Domain.Enums;

namespace BusBooking.Domain.Entities;

public class Booking : BaseEntity
{
    public string BookingReference { get; set; } = string.Empty;  // e.g. "BB-20240501-XXXX"
    public Guid UserId { get; set; }
    public Guid BusId { get; set; }
    public DateTime JourneyDate { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    // Pricing breakdown stored at time of booking (immutable after confirmed)
    public decimal BaseFareTotal { get; set; }
    public decimal ConvenienceFee { get; set; }
    public decimal TotalAmount { get; set; }

    // Boarding and drop-off resolved from OperatorLocation at booking time
    public string BoardingAddress { get; set; } = string.Empty;
    public string DropOffAddress { get; set; } = string.Empty;

    // Navigation
    public User User { get; set; } = null!;
    public Bus Bus { get; set; } = null!;
    public ICollection<BookingPassenger> Passengers { get; set; } = new List<BookingPassenger>();
    public Payment? Payment { get; set; }
    public Cancellation? Cancellation { get; set; }
}