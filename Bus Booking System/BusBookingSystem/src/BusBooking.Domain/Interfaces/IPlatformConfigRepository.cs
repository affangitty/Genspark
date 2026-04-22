using BusBooking.Domain.Entities;

namespace BusBooking.Domain.Interfaces;

public interface IPlatformConfigRepository
{
    Task<PlatformConfig?> GetCurrentAsync();
    void Update(PlatformConfig config);
}
