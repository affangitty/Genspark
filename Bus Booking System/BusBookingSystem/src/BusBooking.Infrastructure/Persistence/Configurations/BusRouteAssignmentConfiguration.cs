using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class BusRouteAssignmentConfiguration : IEntityTypeConfiguration<BusRouteAssignment>
{
    public void Configure(EntityTypeBuilder<BusRouteAssignment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.BaseFare).HasColumnType("decimal(10,2)");
        builder.Property(a => a.AdminNotes).HasMaxLength(500);

        builder.HasOne(a => a.Bus)
               .WithMany()
               .HasForeignKey(a => a.BusId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Route)
               .WithMany()
               .HasForeignKey(a => a.RouteId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}