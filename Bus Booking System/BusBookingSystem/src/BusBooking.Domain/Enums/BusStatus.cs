namespace BusBooking.Domain.Enums;

public enum BusStatus
{
    PendingApproval = 0,    // Submitted by operator, awaiting admin approval
    Active = 1,             // Approved and operational
    TemporarilyUnavailable = 2,  // Operator marked as temp unavailable
    Removed = 3             // Permanently removed by operator
}