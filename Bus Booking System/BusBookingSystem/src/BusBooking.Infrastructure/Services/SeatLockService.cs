using BusBooking.Application.Interfaces;
namespace BusBooking.Infrastructure.Services;
public class SeatLockService : ISeatLockService
{
    public Task<bool> LockSeatAsync(Guid seatId, Guid userId, int lockDurationSeconds = 600) => Task.FromResult(true);
    public Task<bool> UnlockSeatAsync(Guid seatId, Guid userId) => Task.FromResult(true);
    public Task<bool> IsSeatLockedAsync(Guid seatId) => Task.FromResult(false);
    public Task<bool> ExtendLockAsync(Guid seatId, int additionalSeconds = 300) => Task.FromResult(true);
    public Task CleanupExpiredLocksAsync() => Task.CompletedTask;
}
