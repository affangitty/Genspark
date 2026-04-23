namespace BusBooking.Application.Interfaces;

/// <summary>
/// Seat Locking Service for real-time seat reservations
/// </summary>
public interface ISeatLockService
{
    Task<bool> LockSeatAsync(Guid seatId, Guid userId, int lockDurationSeconds = 600, DateTime? journeyDate = null);
    Task<bool> UnlockSeatAsync(Guid seatId, Guid userId, DateTime? journeyDate = null);
    Task<bool> IsSeatLockedAsync(Guid seatId, DateTime? journeyDate = null);
    Task<bool> ExtendLockAsync(Guid seatId, int additionalSeconds = 300, DateTime? journeyDate = null);
    Task CleanupExpiredLocksAsync();
}
