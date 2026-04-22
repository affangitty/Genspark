using BusBooking.Domain.Common;
using BusBooking.Domain.Enums;

namespace BusBooking.Domain.Entities;

public class BusOperator : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string ContactPersonName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public OperatorStatus Status { get; set; } = OperatorStatus.Pending;
    public string? RejectionReason { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? DisabledAt { get; set; }

    // Navigation
    public ICollection<OperatorLocation> Locations { get; set; } = new List<OperatorLocation>();
    public ICollection<Bus> Buses { get; set; } = new List<Bus>();
}