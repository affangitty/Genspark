using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class CancellationConfiguration : IEntityTypeConfiguration<Cancellation>
{
    public void Configure(EntityTypeBuilder<Cancellation> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Reason).HasMaxLength(500);
        builder.Property(c => c.RefundPercentage).HasColumnType("decimal(5,2)");
        builder.Property(c => c.RefundAmount).HasColumnType("decimal(10,2)");
    }
}