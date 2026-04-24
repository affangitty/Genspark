using BusBooking.Domain.Entities;

namespace BusBooking.Domain.Interfaces;

public interface IBusRouteAssignmentRepository
{
    Task<BusRouteAssignment?> GetByIdAsync(Guid id);
    Task<IEnumerable<BusRouteAssignment>> GetPendingAsync();
    Task<IEnumerable<BusRouteAssignment>> GetApprovedAsync();
    Task AddAsync(BusRouteAssignment assignment);
    void Update(BusRouteAssignment assignment);
}