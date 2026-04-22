using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class BusOperatorConfiguration : IEntityTypeConfiguration<BusOperator>
{
    public void Configure(EntityTypeBuilder<BusOperator> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Email).IsRequired().HasMaxLength(200);
        builder.HasIndex(o => o.Email).IsUnique();
        builder.Property(o => o.PhoneNumber).HasMaxLength(15);
        builder.Property(o => o.PasswordHash).IsRequired();
        builder.Property(o => o.Status).IsRequired();
        builder.Property(o => o.RejectionReason).HasMaxLength(500);

        builder.HasMany(o => o.Locations)
               .WithOne(l => l.Operator)
               .HasForeignKey(l => l.OperatorId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Buses)
               .WithOne(b => b.Operator)
               .HasForeignKey(b => b.OperatorId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}