using BusBooking.Domain.Entities;

namespace BusBooking.Domain.Interfaces;

public interface IBusLayoutRepository
{
    Task<BusLayout?> GetByIdAsync(Guid id);
    Task<IEnumerable<BusLayout>> GetByOperatorIdAsync(Guid operatorId);
    Task AddAsync(BusLayout layout);
    void Update(BusLayout layout);
}
