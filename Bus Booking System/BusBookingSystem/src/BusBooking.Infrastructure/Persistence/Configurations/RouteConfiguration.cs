using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Route = BusBooking.Domain.Entities.Route;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.SourceCity).IsRequired().HasMaxLength(100);
        builder.Property(r => r.DestinationCity).IsRequired().HasMaxLength(100);
        builder.Property(r => r.SourceState).HasMaxLength(100);
        builder.Property(r => r.DestinationState).HasMaxLength(100);

        // Prevent duplicate source-destination pairs
        builder.HasIndex(r => new { r.SourceCity, r.DestinationCity }).IsUnique();

        builder.HasMany(r => r.Buses)
               .WithOne(b => b.Route)
               .HasForeignKey(b => b.RouteId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}