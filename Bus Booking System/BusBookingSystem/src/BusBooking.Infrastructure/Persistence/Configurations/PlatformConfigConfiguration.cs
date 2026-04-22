using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class PlatformConfigConfiguration : IEntityTypeConfiguration<PlatformConfig>
{
    public void Configure(EntityTypeBuilder<PlatformConfig> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ConvenienceFeePercentage).HasColumnType("decimal(5,2)");
        builder.Property(p => p.UpdatedByAdminId).HasMaxLength(100);
    }
}