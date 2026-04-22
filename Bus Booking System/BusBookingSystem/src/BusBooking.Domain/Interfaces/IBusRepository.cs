using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;

namespace BusBooking.Domain.Interfaces;

/// <summary>
/// Repository interface for Bus entity
/// </summary>
public interface IBusRepository
{
    Task<Bus?> GetByIdAsync(Guid id);
    Task<Bus?> GetByBusNumberAsync(string busNumber);
    Task<IEnumerable<Bus>> GetByRouteIdAsync(Guid routeId);
    Task<IEnumerable<Bus>> GetByOperatorIdAsync(Guid operatorId);
    Task<IEnumerable<Bus>> GetAllActiveAsync();
    Task<IEnumerable<Bus>> GetByStatusAsync(BusStatus status);
    Task AddAsync(Bus bus);
    void Update(Bus bus);
    void Delete(Bus bus);
}
