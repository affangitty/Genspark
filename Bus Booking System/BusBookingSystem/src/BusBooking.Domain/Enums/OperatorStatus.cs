namespace BusBooking.Domain.Enums;

public enum OperatorStatus
{
    Pending = 0,      // Registered, awaiting admin approval
    Approved = 1,     // Can log in and operate
    Rejected = 2,     // Registration rejected
    Disabled = 3      // Was approved, now disabled by admin
}