using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class BusLayoutConfiguration : IEntityTypeConfiguration<BusLayout>
{
    public void Configure(EntityTypeBuilder<BusLayout> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.LayoutName).IsRequired().HasMaxLength(100);
        builder.Property(l => l.LayoutJson).IsRequired().HasColumnType("jsonb");
        builder.Property(l => l.TotalSeats).IsRequired();
    }
}