using BusBooking.Domain.Entities;

namespace BusBooking.Domain.Interfaces;

public interface ISeatLockRepository
{
    Task<IEnumerable<Guid>> GetActiveLockSeatIdsByBusAndDateAsync(Guid busId, DateTime journeyDate);
    Task<SeatLock?> GetActiveLockAsync(Guid seatId, DateTime journeyDate);
    Task AddAsync(SeatLock seatLock);
    void Update(SeatLock seatLock);
    Task DeleteExpiredLocksAsync();
}
