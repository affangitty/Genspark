using BusBooking.Application.DTOs.Bus;
using MediatR;

namespace BusBooking.Application.Features.Search.Queries;

/// <summary>
/// Query to search available buses with fuzzy location matching
/// </summary>
public class SearchBusesQuery : IRequest<List<BusResponseDto>>
{
    public string SourceCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public DateTime JourneyDate { get; set; }
    public int? PassengerCount { get; set; }
}
