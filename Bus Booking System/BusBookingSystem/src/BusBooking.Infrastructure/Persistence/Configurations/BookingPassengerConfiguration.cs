using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class BookingPassengerConfiguration : IEntityTypeConfiguration<BookingPassenger>
{
    public void Configure(EntityTypeBuilder<BookingPassenger> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PassengerName).IsRequired().HasMaxLength(150);
        builder.Property(p => p.Gender).HasMaxLength(10);

        builder.HasOne(p => p.Seat)
               .WithMany(s => s.BookingPassengers)
               .HasForeignKey(p => p.SeatId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}