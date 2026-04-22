using BusBooking.Domain.Entities;

namespace BusBooking.Domain.Interfaces;

/// <summary>
/// Repository interface for Route entity
/// </summary>
public interface IRouteRepository
{
    Task<Route?> GetByIdAsync(Guid id);
    Task<Route?> GetBySourceDestinationAsync(string source, string destination);
    Task<IEnumerable<Route>> GetAllActiveAsync();
    Task<IEnumerable<Route>> GetAllAsync();
    Task AddAsync(Route route);
    void Update(Route route);
    void Delete(Route route);
}
