using BusBooking.Domain.Common;

namespace BusBooking.Domain.Entities;

public class Cancellation : BaseEntity
{
    public Guid BookingId { get; set; }
    public DateTime CancelledAt { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;

    // Refund calculation
    public decimal RefundPercentage { get; set; }   // 100, 50, or 0
    public decimal RefundAmount { get; set; }
    public bool IsAdminInitiated { get; set; } = false;  // true when operator is disabled

    // Navigation
    public Booking Booking { get; set; } = null!;
}