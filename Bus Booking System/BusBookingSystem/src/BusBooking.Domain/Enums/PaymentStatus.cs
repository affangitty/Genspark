namespace BusBooking.Domain.Enums;

public enum PaymentStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2,
    Refunded = 3,
    PartialRefund = 4
}