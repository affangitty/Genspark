namespace BusBooking.Application.DTOs.Booking;

public class SeatDto
{
    public Guid Id { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Column { get; set; }
    public string Deck { get; set; } = string.Empty;
    public string SeatType { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public bool IsLocked { get; set; }
}

public class PassengerDto
{
    public string PassengerName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public Guid SeatId { get; set; }
}

public class CreateBookingRequestDto
{
    public Guid BusId { get; set; }
    public DateTime JourneyDate { get; set; }
    public List<PassengerDto> Passengers { get; set; } = new();
}

public class BookingResponseDto
{
    public Guid Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string BusNumber { get; set; } = string.Empty;
    public string BusName { get; set; } = string.Empty;
    public string SourceCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public DateTime JourneyDate { get; set; }
    public string BoardingAddress { get; set; } = string.Empty;
    public string DropOffAddress { get; set; } = string.Empty;
    public decimal BaseFareTotal { get; set; }
    public decimal ConvenienceFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<PassengerResponseDto> Passengers { get; set; } = new();
}

public class PassengerResponseDto
{
    public string PassengerName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
}

public class CancelBookingRequestDto
{
    public string Reason { get; set; } = string.Empty;
}

public class CancellationResponseDto
{
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public decimal RefundPercentage { get; set; }
    public decimal RefundAmount { get; set; }
    public DateTime CancelledAt { get; set; }
}

public class SeatLockRequestDto
{
    public Guid SeatId { get; set; }
    public Guid BusId { get; set; }
    public DateTime JourneyDate { get; set; }
}