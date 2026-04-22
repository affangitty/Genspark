using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class SeatLockConfiguration : IEntityTypeConfiguration<SeatLock>
{
    public void Configure(EntityTypeBuilder<SeatLock> builder)
    {
        builder.HasKey(sl => sl.Id);

        // Index for fast lookup: "is this seat locked for this journey date?"
        builder.HasIndex(sl => new { sl.SeatId, sl.JourneyDate, sl.IsReleased });

        builder.HasOne(sl => sl.Seat)
               .WithMany(s => s.SeatLocks)
               .HasForeignKey(sl => sl.SeatId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sl => sl.User)
               .WithMany()
               .HasForeignKey(sl => sl.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}