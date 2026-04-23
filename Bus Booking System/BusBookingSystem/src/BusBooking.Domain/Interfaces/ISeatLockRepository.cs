using BusBooking.Domain.Entities;

namespace BusBooking.Domain.Interfaces;

public interface ISeatLockRepository
{
    /// <param name="exceptUserId">When set, locks held by this user are ignored (for checkout / seat map).</param>
    Task<IEnumerable<Guid>> GetActiveLockSeatIdsByBusAndDateAsync(
        Guid busId, DateTime journeyDate, Guid? exceptUserId = null);
    Task<SeatLock?> GetActiveLockAsync(Guid seatId, DateTime journeyDate);
    Task AddAsync(SeatLock seatLock);
    void Update(SeatLock seatLock);
    Task ReleaseLocksForSeatsAsync(IEnumerable<Guid> seatIds, DateTime journeyDate);
    Task DeleteExpiredLocksAsync();
}
