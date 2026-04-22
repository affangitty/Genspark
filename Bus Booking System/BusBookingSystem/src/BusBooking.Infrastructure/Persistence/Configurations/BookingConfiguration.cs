using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BookingReference).IsRequired().HasMaxLength(30);
        builder.HasIndex(b => b.BookingReference).IsUnique();
        builder.Property(b => b.BaseFareTotal).HasColumnType("decimal(10,2)");
        builder.Property(b => b.ConvenienceFee).HasColumnType("decimal(10,2)");
        builder.Property(b => b.TotalAmount).HasColumnType("decimal(10,2)");
        builder.Property(b => b.BoardingAddress).HasMaxLength(400);
        builder.Property(b => b.DropOffAddress).HasMaxLength(400);

        builder.HasMany(b => b.Passengers)
               .WithOne(p => p.Booking)
               .HasForeignKey(p => p.BookingId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Payment)
               .WithOne(p => p.Booking)
               .HasForeignKey<Payment>(p => p.BookingId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Cancellation)
               .WithOne(c => c.Booking)
               .HasForeignKey<Cancellation>(c => c.BookingId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}