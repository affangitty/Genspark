using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SeatNumber).IsRequired().HasMaxLength(10);
        builder.Property(s => s.Deck).HasMaxLength(10);

        // Each bus can only have one seat with a given number
        builder.HasIndex(s => new { s.BusId, s.SeatNumber }).IsUnique();
    }
}