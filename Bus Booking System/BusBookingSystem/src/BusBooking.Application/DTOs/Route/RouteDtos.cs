namespace BusBooking.Application.DTOs.Route;

public class CreateRouteRequestDto
{
    public string SourceCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public string SourceState { get; set; } = string.Empty;
    public string DestinationState { get; set; } = string.Empty;
}

public class RouteResponseDto
{
    public Guid Id { get; set; }
    public string SourceCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public string SourceState { get; set; } = string.Empty;
    public string DestinationState { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}