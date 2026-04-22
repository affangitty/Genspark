using BusBooking.Domain.Common;
using BusBooking.Domain.Enums;

namespace BusBooking.Domain.Entities;

public class Bus : BaseEntity
{
    public string BusNumber { get; set; } = string.Empty;   // Unique vehicle reg number
    public string BusName { get; set; } = string.Empty;     // Display name e.g. "Sharma Travels Express"
    public BusStatus Status { get; set; } = BusStatus.PendingApproval;

    public Guid OperatorId { get; set; }
    public Guid LayoutId { get; set; }
    public Guid? RouteId { get; set; }          // Null until assigned to a route

    // Schedule
    public TimeSpan? DepartureTime { get; set; }
    public TimeSpan? ArrivalTime { get; set; }
    public int? DurationMinutes { get; set; }

    // Pricing (operator sets base price; platform fee added on top at booking)
    public decimal BaseFare { get; set; }

    public DateTime? ApprovedAt { get; set; }
    public string? AdminNotes { get; set; }

    // Navigation
    public BusOperator Operator { get; set; } = null!;
    public BusLayout Layout { get; set; } = null!;
    public Route? Route { get; set; }
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}