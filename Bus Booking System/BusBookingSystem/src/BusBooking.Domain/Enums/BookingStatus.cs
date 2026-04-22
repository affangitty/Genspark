namespace BusBooking.Domain.Enums;

public enum BookingStatus
{
    Pending = 0,        // Payment not yet completed
    Confirmed = 1,      // Payment successful
    Cancelled = 2,      // Cancelled by user
    CancelledByAdmin = 3 // Cancelled due to operator being disabled
}