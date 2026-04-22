using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Route = BusBooking.Domain.Entities.Route;

namespace BusBooking.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<BusOperator> BusOperators => Set<BusOperator>();
    public DbSet<OperatorLocation> OperatorLocations => Set<OperatorLocation>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<BusLayout> BusLayouts => Set<BusLayout>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<SeatLock> SeatLocks => Set<SeatLock>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingPassenger> BookingPassengers => Set<BookingPassenger>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Cancellation> Cancellations => Set<Cancellation>();
    public DbSet<PlatformConfig> PlatformConfigs => Set<PlatformConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BusBooking.Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(ct);
    }
}