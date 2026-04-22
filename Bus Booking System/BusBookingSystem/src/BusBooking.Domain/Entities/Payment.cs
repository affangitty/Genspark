using BusBooking.Domain.Common;
using BusBooking.Domain.Enums;

namespace BusBooking.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid BookingId { get; set; }
    public string TransactionId { get; set; } = string.Empty;   // Dummy gateway ref
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string PaymentMethod { get; set; } = "DummyGateway";
    public DateTime? PaidAt { get; set; }
    public decimal? RefundAmount { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? FailureReason { get; set; }

    // Navigation
    public Booking Booking { get; set; } = null!;
}