namespace BusBooking.Application.DTOs.Booking;

/// <summary>
/// DTO for seat status information (availability, lock status)
/// </summary>
public class SeatStatusDto
{
    public Guid SeatId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public bool IsBooked { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedUntil { get; set; }
    public Guid? LockedByUserId { get; set; }
}

/// <summary>
/// DTO for bus seat layout and availability on a specific date
/// </summary>
public class BusSeatAvailabilityDto
{
    public Guid BusId { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public DateTime JourneyDate { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public int BookedSeats { get; set; }
    public int LockedSeats { get; set; }
    public List<SeatStatusDto> Seats { get; set; } = new();
}

/// <summary>
/// Response for payment processing
/// </summary>
public class PaymentResponseDto
{
    public Guid BookingId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ProcessedAt { get; set; }
}

/// <summary>
/// DTO for cancellation refund details
/// </summary>
public class RefundDetailDto
{
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal RefundPercentage { get; set; }
    public decimal RefundAmount { get; set; }
    public string RefundReason { get; set; } = string.Empty;
    public DateTime CancelledAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// DTO for getting passenger manifest for a bus
/// </summary>
public class BusManifestDto
{
    public Guid BusId { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public string BusName { get; set; } = string.Empty;
    public DateTime JourneyDate { get; set; }
    public int TotalSeats { get; set; }
    public int ConfirmedPassengers { get; set; }
    public List<ManifestPassengerDto> Passengers { get; set; } = new();
}

public class ManifestPassengerDto
{
    public string PassengerName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public string BookingReference { get; set; } = string.Empty;
}

public class OperatorBookingDto
{
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public DateTime JourneyDate { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string BusNumber { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<ManifestPassengerDto> Passengers { get; set; } = new();
}

public class OperatorBookingSummaryDto
{
    public int TotalBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal NetRevenue { get; set; }
}
