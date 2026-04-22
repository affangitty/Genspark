namespace BusBooking.Application.Interfaces;

/// <summary>
/// Seat Locking Service for real-time seat reservations
/// </summary>
public interface ISeatLockService
{
    Task<bool> LockSeatAsync(Guid seatId, Guid userId, int lockDurationSeconds = 600);
    Task<bool> UnlockSeatAsync(Guid seatId, Guid userId);
    Task<bool> IsSeatLockedAsync(Guid seatId);
    Task<bool> ExtendLockAsync(Guid seatId, int additionalSeconds = 300);
    Task CleanupExpiredLocksAsync();
}
