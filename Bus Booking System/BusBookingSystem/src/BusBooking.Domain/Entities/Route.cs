using BusBooking.Domain.Common;

namespace BusBooking.Domain.Entities;

/// <summary>
/// Admin-defined point-to-point route. No intermediate stops.
/// </summary>
public class Route : BaseEntity
{
    public string SourceCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public string SourceState { get; set; } = string.Empty;
    public string DestinationState { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Bus> Buses { get; set; } = new List<Bus>();
}