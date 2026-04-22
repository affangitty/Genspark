using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.TransactionId).HasMaxLength(100);
        builder.Property(p => p.Amount).HasColumnType("decimal(10,2)");
        builder.Property(p => p.RefundAmount).HasColumnType("decimal(10,2)");
        builder.Property(p => p.PaymentMethod).HasMaxLength(50);
        builder.Property(p => p.FailureReason).HasMaxLength(300);
    }
}