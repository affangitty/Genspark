using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using BusBooking.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Route = BusBooking.Domain.Entities.Route;

namespace BusBooking.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _context.Users.FindAsync(id);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<IEnumerable<User>> GetAllAsync() =>
        await _context.Users.ToListAsync();

    public async Task AddAsync(User user) =>
        await _context.Users.AddAsync(user);

    public void Update(User user) =>
        _context.Users.Update(user);

    public void Delete(User user) =>
        _context.Users.Remove(user);

    public async Task<bool> ExistsAsync(Guid id) =>
        await _context.Users.AnyAsync(u => u.Id == id);
}

public class BusRepository : IBusRepository
{
    private readonly AppDbContext _context;

    public BusRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Bus?> GetByIdAsync(Guid id) =>
        await _context.Buses
            .Include(b => b.Operator)
            .Include(b => b.Layout)
            .Include(b => b.Route)
            .Include(b => b.Seats)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<Bus?> GetByBusNumberAsync(string busNumber) =>
        await _context.Buses
            .Include(b => b.Operator)
            .Include(b => b.Layout)
            .Include(b => b.Route)
            .FirstOrDefaultAsync(b => b.BusNumber == busNumber);

    public async Task<IEnumerable<Bus>> GetByRouteIdAsync(Guid routeId) =>
        await _context.Buses
            .Include(b => b.Operator)
            .Include(b => b.Layout)
            .Include(b => b.Route)
            .Include(b => b.Seats)
            .Where(b => b.RouteId == routeId)
            .ToListAsync();

    public async Task<IEnumerable<Bus>> GetByOperatorIdAsync(Guid operatorId) =>
        await _context.Buses
            .Include(b => b.Layout)
            .Include(b => b.Route)
            .Where(b => b.OperatorId == operatorId)
            .ToListAsync();

    public async Task<IEnumerable<Bus>> GetAllActiveAsync() =>
        await _context.Buses
            .Include(b => b.Operator)
            .Include(b => b.Layout)
            .Include(b => b.Route)
            .Where(b => b.Status == BusStatus.Active)
            .ToListAsync();

    public async Task<IEnumerable<Bus>> GetByStatusAsync(BusStatus status) =>
        await _context.Buses
            .Include(b => b.Operator)
            .Include(b => b.Layout)
            .Include(b => b.Route)
            .Where(b => b.Status == status)
            .ToListAsync();

    public async Task AddAsync(Bus bus) =>
        await _context.Buses.AddAsync(bus);

    public void Update(Bus bus) =>
        _context.Buses.Update(bus);

    public void Delete(Bus bus) =>
        _context.Buses.Remove(bus);
}

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(Guid id) =>
        await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Bus).ThenInclude(bus => bus.Route)
            .Include(b => b.Bus).ThenInclude(bus => bus.Operator)
            .Include(b => b.Passengers).ThenInclude(p => p.Seat)
            .Include(b => b.Payment)
            .Include(b => b.Cancellation)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<IEnumerable<Booking>> GetByUserIdAsync(Guid userId) =>
        await _context.Bookings
            .Include(b => b.Bus).ThenInclude(bus => bus.Route)
            .Include(b => b.Bus).ThenInclude(bus => bus.Operator)
            .Include(b => b.Passengers).ThenInclude(p => p.Seat)
            .Include(b => b.Payment)
            .Include(b => b.Cancellation)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<Booking>> GetByBusIdAsync(Guid busId) =>
        await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Passengers).ThenInclude(p => p.Seat)
            .Include(b => b.Payment)
            .Where(b => b.BusId == busId)
            .OrderByDescending(b => b.JourneyDate)
            .ToListAsync();

    public async Task<IEnumerable<Booking>> GetByDateRangeAsync(DateTime startDate, DateTime endDate) =>
        await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Bus).ThenInclude(bus => bus.Route)
            .Include(b => b.Passengers)
            .Include(b => b.Payment)
            .Where(b => b.JourneyDate >= startDate && b.JourneyDate <= endDate)
            .OrderByDescending(b => b.JourneyDate)
            .ToListAsync();

    public async Task<IEnumerable<Booking>> GetFutureBookingsByBusIdAsync(Guid busId) =>
        await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Passengers)
            .Include(b => b.Payment)
            .Include(b => b.Cancellation)
            .Where(b => b.BusId == busId
                && b.JourneyDate > DateTime.UtcNow
                && b.Status == BookingStatus.Confirmed)
            .ToListAsync();

    public async Task<IEnumerable<Booking>> GetUpcomingByUserIdAsync(Guid userId, DateTime currentTimeUtc) =>
        await _context.Bookings
            .Include(b => b.Bus).ThenInclude(bus => bus.Route)
            .Include(b => b.Bus).ThenInclude(bus => bus.Operator)
            .Include(b => b.Passengers).ThenInclude(p => p.Seat)
            .Include(b => b.Payment)
            .Where(b => b.UserId == userId
                && b.JourneyDate >= currentTimeUtc
                && b.Status == BookingStatus.Confirmed)
            .OrderBy(b => b.JourneyDate)
            .ToListAsync();

    public async Task<IEnumerable<Booking>> GetPastByUserIdAsync(Guid userId, DateTime currentTimeUtc) =>
        await _context.Bookings
            .Include(b => b.Bus).ThenInclude(bus => bus.Route)
            .Include(b => b.Bus).ThenInclude(bus => bus.Operator)
            .Include(b => b.Passengers).ThenInclude(p => p.Seat)
            .Include(b => b.Payment)
            .Where(b => b.UserId == userId
                && b.JourneyDate < currentTimeUtc
                && b.Status == BookingStatus.Confirmed)
            .OrderByDescending(b => b.JourneyDate)
            .ToListAsync();

    public async Task<IEnumerable<Booking>> GetCancelledByUserIdAsync(Guid userId) =>
        await _context.Bookings
            .Include(b => b.Bus).ThenInclude(bus => bus.Route)
            .Include(b => b.Bus).ThenInclude(bus => bus.Operator)
            .Include(b => b.Passengers).ThenInclude(p => p.Seat)
            .Include(b => b.Payment)
            .Include(b => b.Cancellation)
            .Where(b => b.UserId == userId
                && (b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.CancelledByAdmin))
            .OrderByDescending(b => b.UpdatedAt)
            .ToListAsync();

    public async Task<IEnumerable<Booking>> GetByOperatorIdAsync(Guid operatorId) =>
        await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Bus).ThenInclude(bus => bus.Route)
            .Include(b => b.Passengers).ThenInclude(p => p.Seat)
            .Include(b => b.Payment)
            .Include(b => b.Cancellation)
            .Where(b => b.Bus.OperatorId == operatorId)
            .OrderByDescending(b => b.JourneyDate)
            .ToListAsync();

    public async Task<Booking?> GetByReferenceAsync(string bookingReference) =>
        await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Bus).ThenInclude(bus => bus.Route)
            .Include(b => b.Bus).ThenInclude(bus => bus.Operator)
            .Include(b => b.Passengers).ThenInclude(p => p.Seat)
            .Include(b => b.Payment)
            .Include(b => b.Cancellation)
            .FirstOrDefaultAsync(b => b.BookingReference == bookingReference);

    public async Task<IEnumerable<Guid>> GetBookedSeatIdsByBusAndDateAsync(
        Guid busId, DateTime journeyDate) =>
        await _context.BookingPassengers
            .Include(bp => bp.Booking)
            .Where(bp => bp.Booking.BusId == busId
                && bp.Booking.JourneyDate.Date == journeyDate.Date
                && bp.Booking.Status == BookingStatus.Confirmed)
            .Select(bp => bp.SeatId)
            .ToListAsync();

    public async Task AddAsync(Booking booking) =>
        await _context.Bookings.AddAsync(booking);

    public async Task AddPassengerAsync(BookingPassenger passenger) =>
        await _context.BookingPassengers.AddAsync(passenger);

    public async Task AddPaymentAsync(Payment payment) =>
        await _context.Payments.AddAsync(payment);

    public async Task AddCancellationAsync(Cancellation cancellation) =>
        await _context.Cancellations.AddAsync(cancellation);

    public void Update(Booking booking) =>
        _context.Bookings.Update(booking);

    public void Delete(Booking booking) =>
        _context.Bookings.Remove(booking);
}

public class RouteRepository : IRouteRepository
{
    private readonly AppDbContext _context;

    public RouteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Route?> GetByIdAsync(Guid id) =>
        await _context.Routes.FindAsync(id);

    public async Task<Route?> GetBySourceDestinationAsync(string source, string destination) =>
        await _context.Routes.FirstOrDefaultAsync(r =>
            r.SourceCity.ToLower() == source.ToLower() &&
            r.DestinationCity.ToLower() == destination.ToLower());

    public async Task<IEnumerable<Route>> GetAllActiveAsync() =>
        await _context.Routes
            .Where(r => r.IsActive)
            .OrderBy(r => r.SourceCity)
            .ToListAsync();

    public async Task<IEnumerable<Route>> GetAllAsync() =>
        await _context.Routes
            .OrderBy(r => r.SourceCity)
            .ToListAsync();

    public async Task AddAsync(Route route) =>
        await _context.Routes.AddAsync(route);

    public void Update(Route route) =>
        _context.Routes.Update(route);

    public void Delete(Route route) =>
        _context.Routes.Remove(route);
}

public class BusOperatorRepository : IBusOperatorRepository
{
    private readonly AppDbContext _context;

    public BusOperatorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BusOperator?> GetByIdAsync(Guid id) =>
        await _context.BusOperators
            .Include(o => o.Locations)
            .Include(o => o.Buses)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<BusOperator?> GetByEmailAsync(string email) =>
        await _context.BusOperators
            .FirstOrDefaultAsync(o => o.Email == email);

    public async Task<IEnumerable<BusOperator>> GetAllAsync() =>
        await _context.BusOperators
            .Include(o => o.Locations)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<BusOperator>> GetByStatusAsync(OperatorStatus status) =>
        await _context.BusOperators
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(BusOperator busOperator) =>
        await _context.BusOperators.AddAsync(busOperator);

    public void Update(BusOperator busOperator) =>
        _context.BusOperators.Update(busOperator);

    public async Task AddLocationAsync(OperatorLocation location) =>
        await _context.OperatorLocations.AddAsync(location);

    public async Task<IEnumerable<OperatorLocation>> GetLocationsByOperatorIdAsync(Guid operatorId) =>
        await _context.OperatorLocations
            .Where(l => l.OperatorId == operatorId)
            .ToListAsync();

    public async Task<OperatorLocation?> GetLocationByIdAsync(Guid locationId) =>
        await _context.OperatorLocations.FindAsync(locationId);

    public void RemoveLocation(OperatorLocation location) =>
        _context.OperatorLocations.Remove(location);
}

public class PlatformConfigRepository : IPlatformConfigRepository
{
    private readonly AppDbContext _context;

    public PlatformConfigRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PlatformConfig?> GetCurrentAsync() =>
        await _context.PlatformConfigs.FirstOrDefaultAsync();

    public void Update(PlatformConfig config) =>
        _context.PlatformConfigs.Update(config);
}

public class BusLayoutRepository : IBusLayoutRepository
{
    private readonly AppDbContext _context;
    public BusLayoutRepository(AppDbContext context) => _context = context;

    public async Task<BusLayout?> GetByIdAsync(Guid id) =>
        await _context.BusLayouts.FindAsync(id);

    public async Task<IEnumerable<BusLayout>> GetByOperatorIdAsync(Guid operatorId) =>
        await _context.BusLayouts
            .Where(l => l.OperatorId == operatorId)
            .ToListAsync();

    public async Task AddAsync(BusLayout layout) =>
        await _context.BusLayouts.AddAsync(layout);

    public void Update(BusLayout layout) =>
        _context.BusLayouts.Update(layout);
}

public class SeatRepository : ISeatRepository
{
    private readonly AppDbContext _context;
    public SeatRepository(AppDbContext context) => _context = context;

    public async Task<Seat?> GetByIdAsync(Guid id) =>
        await _context.Seats.FindAsync(id);

    public async Task<IEnumerable<Seat>> GetByBusIdAsync(Guid busId) =>
        await _context.Seats
            .Where(s => s.BusId == busId && s.IsActive)
            .OrderBy(s => s.Row).ThenBy(s => s.Column)
            .ToListAsync();

    public async Task AddRangeAsync(IEnumerable<Seat> seats) =>
        await _context.Seats.AddRangeAsync(seats);
}

public class SeatLockRepository : ISeatLockRepository
{
    private readonly AppDbContext _context;
    public SeatLockRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Guid>> GetActiveLockSeatIdsByBusAndDateAsync(
        Guid busId, DateTime journeyDate, Guid? exceptUserId = null)
    {
        var q = _context.SeatLocks
            .Where(sl => sl.BusId == busId
                && sl.JourneyDate.Date == journeyDate.Date
                && !sl.IsReleased
                && sl.ExpiresAt > DateTime.UtcNow);
        if (exceptUserId.HasValue)
            q = q.Where(sl => sl.UserId != exceptUserId.Value);
        return await q.Select(sl => sl.SeatId).ToListAsync();
    }

    public async Task<SeatLock?> GetActiveLockAsync(Guid seatId, DateTime journeyDate) =>
        await _context.SeatLocks
            .FirstOrDefaultAsync(sl => sl.SeatId == seatId
                && sl.JourneyDate.Date == journeyDate.Date
                && !sl.IsReleased
                && sl.ExpiresAt > DateTime.UtcNow);

    public async Task AddAsync(SeatLock seatLock) =>
        await _context.SeatLocks.AddAsync(seatLock);

    public void Update(SeatLock seatLock) =>
        _context.SeatLocks.Update(seatLock);

    public async Task ReleaseLocksForSeatsAsync(IEnumerable<Guid> seatIds, DateTime journeyDate)
    {
        var targetIds = seatIds.ToHashSet();
        if (targetIds.Count == 0)
            return;

        var activeLocks = await _context.SeatLocks
            .Where(sl => targetIds.Contains(sl.SeatId)
                && sl.JourneyDate.Date == journeyDate.Date
                && !sl.IsReleased
                && sl.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var seatLock in activeLocks)
        {
            seatLock.IsReleased = true;
        }
    }

    public async Task DeleteExpiredLocksAsync()
    {
        var expired = await _context.SeatLocks
            .Where(sl => !sl.IsReleased && sl.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();
        _context.SeatLocks.RemoveRange(expired);
    }
}

public class BusRouteAssignmentRepository : IBusRouteAssignmentRepository
{
    private readonly AppDbContext _context;
    public BusRouteAssignmentRepository(AppDbContext context) => _context = context;

    public async Task<BusRouteAssignment?> GetByIdAsync(Guid id) =>
        await _context.BusRouteAssignments
            .Include(a => a.Bus).ThenInclude(b => b.Operator)
            .Include(a => a.Route)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IEnumerable<BusRouteAssignment>> GetPendingAsync() =>
        await _context.BusRouteAssignments
            .Include(a => a.Bus).ThenInclude(b => b.Operator)
            .Include(a => a.Route)
            .Where(a => !a.IsApproved && !a.IsRejected)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(BusRouteAssignment assignment) =>
        await _context.BusRouteAssignments.AddAsync(assignment);

    public void Update(BusRouteAssignment assignment) =>
        _context.BusRouteAssignments.Update(assignment);
}