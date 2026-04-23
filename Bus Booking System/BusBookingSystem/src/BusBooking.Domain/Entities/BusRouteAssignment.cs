using BusBooking.Domain.Common;

namespace BusBooking.Domain.Entities;

/// <summary>
/// Tracks operator requests to assign a bus to a route.
/// Admin must approve before the assignment goes live.
/// </summary>
public class BusRouteAssignment : BaseEntity
{
    public Guid BusId { get; set; }
    public Guid RouteId { get; set; }
    public Guid OperatorId { get; set; }
    public TimeSpan DepartureTime { get; set; }
    public TimeSpan ArrivalTime { get; set; }
    public int DurationMinutes { get; set; }
    public decimal BaseFare { get; set; }
    public bool IsApproved { get; set; } = false;
    public bool IsRejected { get; set; } = false;
    public string? AdminNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }

    // Navigation
    public Bus Bus { get; set; } = null!;
    public Route Route { get; set; } = null!;
}