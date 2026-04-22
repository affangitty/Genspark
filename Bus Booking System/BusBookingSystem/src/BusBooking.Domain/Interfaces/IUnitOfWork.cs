namespace BusBooking.Domain.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    IBusRepository Buses { get; }
    IBusLayoutRepository BusLayouts { get; }
    ISeatRepository Seats { get; }
    ISeatLockRepository SeatLocks { get; }
    IBookingRepository Bookings { get; }
    IRouteRepository Routes { get; }
    IBusOperatorRepository BusOperators { get; }
    IPlatformConfigRepository PlatformConfig { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}