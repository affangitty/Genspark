using BusBooking.Domain.Common;

namespace BusBooking.Domain.Entities;

/// <summary>
/// Temporary lock placed on a seat the moment a user clicks it.
/// Expires after a configurable window (e.g. 10 minutes).
/// Prevents double booking during the checkout flow.
/// </summary>
public class SeatLock : BaseEntity
{
    public Guid SeatId { get; set; }
    public Guid UserId { get; set; }
    public Guid BusId { get; set; }
    public DateTime JourneyDate { get; set; }
    public DateTime LockedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsReleased { get; set; } = false;

    // Navigation
    public Seat Seat { get; set; } = null!;
    public User User { get; set; } = null!;
}