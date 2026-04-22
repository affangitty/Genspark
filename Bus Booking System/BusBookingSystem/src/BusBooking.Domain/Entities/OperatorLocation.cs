using BusBooking.Domain.Common;

namespace BusBooking.Domain.Entities;

/// <summary>
/// Represents the operator's office address at a given city/location.
/// This address becomes the boarding/drop-off point for bookings on that route.
/// </summary>
public class OperatorLocation : BaseEntity
{
    public Guid OperatorId { get; set; }
    public string City { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string Landmark { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PinCode { get; set; } = string.Empty;

    // Navigation
    public BusOperator Operator { get; set; } = null!;
}