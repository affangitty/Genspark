// src/BusBooking.Domain/Interfaces/IBusOperatorRepository.cs
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;

namespace BusBooking.Domain.Interfaces;

public interface IBusOperatorRepository
{
    Task<BusOperator?> GetByIdAsync(Guid id);
    Task<BusOperator?> GetByEmailAsync(string email);
    Task<IEnumerable<BusOperator>> GetAllAsync();
    Task<IEnumerable<BusOperator>> GetByStatusAsync(OperatorStatus status);
    Task AddAsync(BusOperator busOperator);
    void Update(BusOperator busOperator);

    // Locations
    Task AddLocationAsync(OperatorLocation location);
    Task<IEnumerable<OperatorLocation>> GetLocationsByOperatorIdAsync(Guid operatorId);
    Task<OperatorLocation?> GetLocationByIdAsync(Guid locationId);
    void RemoveLocation(OperatorLocation location);
}