using BusBooking.Domain.Entities;

namespace BusBooking.Domain.Interfaces;

public interface ISeatRepository
{
    Task<Seat?> GetByIdAsync(Guid id);
    Task<IEnumerable<Seat>> GetByBusIdAsync(Guid busId);
    Task AddRangeAsync(IEnumerable<Seat> seats);
}
