using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class OperatorLocationConfiguration : IEntityTypeConfiguration<OperatorLocation>
{
    public void Configure(EntityTypeBuilder<OperatorLocation> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.City).IsRequired().HasMaxLength(100);
        builder.Property(l => l.AddressLine).IsRequired().HasMaxLength(300);
        builder.Property(l => l.State).HasMaxLength(100);
        builder.Property(l => l.PinCode).HasMaxLength(10);
        builder.Property(l => l.Landmark).HasMaxLength(200);
    }
}