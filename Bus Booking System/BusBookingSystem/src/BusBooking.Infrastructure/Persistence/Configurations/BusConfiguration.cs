using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class BusConfiguration : IEntityTypeConfiguration<Bus>
{
    public void Configure(EntityTypeBuilder<Bus> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BusNumber).IsRequired().HasMaxLength(20);
        builder.HasIndex(b => b.BusNumber).IsUnique();
        builder.Property(b => b.BusName).IsRequired().HasMaxLength(200);
        builder.Property(b => b.BaseFare).HasColumnType("decimal(10,2)");
        builder.Property(b => b.Status).IsRequired();

        builder.HasOne(b => b.Layout)
               .WithMany(l => l.Buses)
               .HasForeignKey(b => b.LayoutId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Seats)
               .WithOne(s => s.Bus)
               .HasForeignKey(s => s.BusId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Bookings)
               .WithOne(bk => bk.Bus)
               .HasForeignKey(bk => bk.BusId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}