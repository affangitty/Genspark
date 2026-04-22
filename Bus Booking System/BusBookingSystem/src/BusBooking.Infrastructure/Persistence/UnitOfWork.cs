using BusBooking.Domain.Interfaces;
using BusBooking.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace BusBooking.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    private IUserRepository? _users;
    private IBusRepository? _buses;
    private IBusLayoutRepository? _busLayouts;
    private ISeatRepository? _seats;
    private ISeatLockRepository? _seatLocks;
    private IBookingRepository? _bookings;
    private IRouteRepository? _routes;
    private IBusOperatorRepository? _busOperators;
    private IPlatformConfigRepository? _platformConfig;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users =>
        _users ??= new UserRepository(_context);

    public IBusRepository Buses =>
        _buses ??= new BusRepository(_context);

    public IBusLayoutRepository BusLayouts =>
        _busLayouts ??= new BusLayoutRepository(_context);

    public ISeatRepository Seats =>
        _seats ??= new SeatRepository(_context);

    public ISeatLockRepository SeatLocks =>
        _seatLocks ??= new SeatLockRepository(_context);

    public IBookingRepository Bookings =>
        _bookings ??= new BookingRepository(_context);

    public IRouteRepository Routes =>
        _routes ??= new RouteRepository(_context);

    public IBusOperatorRepository BusOperators =>
        _busOperators ??= new BusOperatorRepository(_context);

    public IPlatformConfigRepository PlatformConfig =>
        _platformConfig ??= new PlatformConfigRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync() =>
        _transaction = await _context.Database.BeginTransactionAsync();

    public async Task CommitAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
            await _transaction.DisposeAsync();
        await _context.DisposeAsync();
    }
}