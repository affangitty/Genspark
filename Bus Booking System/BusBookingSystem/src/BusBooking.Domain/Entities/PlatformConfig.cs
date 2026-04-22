using BusBooking.Domain.Common;

namespace BusBooking.Domain.Entities;

/// <summary>
/// Single-row config table. Admin controls platform fee and seat lock duration.
/// </summary>
public class PlatformConfig : BaseEntity
{
    public decimal ConvenienceFeePercentage { get; set; } = 5.0m;  // % added on top of base fare
    public int SeatLockDurationMinutes { get; set; } = 10;          // How long a seat stays locked
    public string UpdatedByAdminId { get; set; } = string.Empty;
}