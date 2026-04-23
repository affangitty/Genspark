namespace BusBooking.Application.DTOs.Bus;

public class BusSearchRequestDto
{
    public string SourceCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public DateTime JourneyDate { get; set; }
    public int? PassengerCount { get; set; }
}

public class BusResponseDto
{
    public Guid Id { get; set; }
    public string BusNumber { get; set; } = string.Empty;
    public string BusName { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string SourceCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public TimeSpan? DepartureTime { get; set; }
    public TimeSpan? ArrivalTime { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public decimal BaseFare { get; set; }
    public decimal ConvenienceFee { get; set; }
    public decimal TotalFare { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateBusRequestDto
{
    public string BusNumber { get; set; } = string.Empty;
    public string BusName { get; set; } = string.Empty;
    public Guid LayoutId { get; set; }
    public Guid? RouteId { get; set; }
    public TimeSpan? DepartureTime { get; set; }
    public TimeSpan? ArrivalTime { get; set; }
    public decimal BaseFare { get; set; }
}

public class BusLayoutDto
{
    public Guid Id { get; set; }
    public string LayoutName { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public int Rows { get; set; }
    public int Columns { get; set; }
    public bool HasUpperDeck { get; set; }
    public string LayoutJson { get; set; } = "[]";
}

public class UpdateFareRequestDto
{
    public decimal BaseFare { get; set; }
}

public class CreateBusLayoutRequestDto
{
    public string LayoutName { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public int Rows { get; set; }
    public int Columns { get; set; }
    public bool HasUpperDeck { get; set; }
    public string LayoutJson { get; set; } = "[]";
}

public class AssignRouteRequestDto
{
    public Guid RouteId { get; set; }
    public TimeSpan DepartureTime { get; set; }
    public TimeSpan ArrivalTime { get; set; }
    public int DurationMinutes { get; set; }
    public decimal BaseFare { get; set; }
}