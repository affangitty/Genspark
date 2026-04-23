using BusBooking.Application.DTOs.Bus;
using BusBooking.Domain.Enums;
using BusBooking.Domain.Interfaces;
using MediatR;
using AutoMapper;
using System.Text.RegularExpressions;

namespace BusBooking.Application.Features.Search.Queries;

/// <summary>
/// Handler for bus search with fuzzy matching on location names
/// </summary>
public class SearchBusesQueryHandler : IRequestHandler<SearchBusesQuery, List<BusResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SearchBusesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<BusResponseDto>> Handle(SearchBusesQuery request, CancellationToken cancellationToken)
    {
        var allRoutes = await _unitOfWork.Routes.GetAllActiveAsync();
        
        // Fuzzy match on source and destination
        var matchedRoutes = allRoutes.Where(r =>
            FuzzyMatch(r.SourceCity, request.SourceCity) &&
            FuzzyMatch(r.DestinationCity, request.DestinationCity)
        ).ToList();

        var result = new List<BusResponseDto>();

        foreach (var route in matchedRoutes)
        {
            // Get buses assigned to this route for the given date
            var buses = await _unitOfWork.Buses.GetByRouteIdAsync(route.Id);
            
            foreach (var bus in buses)
            {
                if (bus.Status != BusStatus.Active || bus.Operator.Status != OperatorStatus.Approved)
                    continue;

                var bookedSeatIds = await _unitOfWork.Bookings.GetBookedSeatIdsByBusAndDateAsync(bus.Id, request.JourneyDate);
                var lockedSeatIds = await _unitOfWork.SeatLocks.GetActiveLockSeatIdsByBusAndDateAsync(
                    bus.Id, request.JourneyDate, exceptUserId: null);
                var totalSeats = bus.Seats.Count(s => s.IsActive);
                var unavailable = bookedSeatIds.Concat(lockedSeatIds).ToHashSet();
                var availableSeats = totalSeats - unavailable.Count;

                // Skip if not enough seats available for requested passengers
                if (request.PassengerCount.HasValue && availableSeats < request.PassengerCount)
                    continue;

                // Get platform config for convenience fee
                var platformConfig = await _unitOfWork.PlatformConfig.GetCurrentAsync();
                var convenienceFee = platformConfig != null 
                    ? (bus.BaseFare * platformConfig.ConvenienceFeePercentage) / 100 
                    : 0;

                var busDto = new BusResponseDto
                {
                    Id = bus.Id,
                    BusNumber = bus.BusNumber,
                    BusName = bus.BusName,
                    OperatorName = bus.Operator.CompanyName,
                    SourceCity = route.SourceCity,
                    DestinationCity = route.DestinationCity,
                    DepartureTime = bus.DepartureTime,
                    ArrivalTime = bus.ArrivalTime,
                    TotalSeats = totalSeats,
                    AvailableSeats = availableSeats,
                    BaseFare = bus.BaseFare,
                    ConvenienceFee = convenienceFee,
                    TotalFare = bus.BaseFare + convenienceFee,
                    Status = bus.Status.ToString()
                };

                result.Add(busDto);
            }
        }

        return result.OrderBy(b => b.TotalFare).ToList();
    }

    /// <summary>
    /// Fuzzy match algorithm: checks if source matches destination with tolerance
    /// Uses Levenshtein distance for similarity matching
    /// </summary>
    private bool FuzzyMatch(string source, string target, double threshold = 0.8)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return false;

        source = NormalizeName(source);
        target = NormalizeName(target);

        if (source == target)
            return true;

        // Levenshtein distance based matching
        var distance = LevenshteinDistance(source, target);
        var maxLength = Math.Max(source.Length, target.Length);
        var similarity = 1.0 - (double)distance / maxLength;

        return similarity >= threshold;
    }

    /// <summary>
    /// Normalize city names for comparison (lowercase, trim, remove extra spaces)
    /// </summary>
    private string NormalizeName(string name)
    {
        return Regex.Replace(name.ToLowerInvariant().Trim(), @"\s+", " ");
    }

    /// <summary>
    /// Calculate Levenshtein distance between two strings
    /// </summary>
    private int LevenshteinDistance(string s1, string s2)
    {
        var m = s1.Length;
        var n = s2.Length;
        var dp = new int[m + 1, n + 1];

        for (int i = 0; i <= m; i++)
            dp[i, 0] = i;

        for (int j = 0; j <= n; j++)
            dp[0, j] = j;

        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                if (s1[i - 1] == s2[j - 1])
                    dp[i, j] = dp[i - 1, j - 1];
                else
                    dp[i, j] = 1 + Math.Min(Math.Min(dp[i - 1, j], dp[i, j - 1]), dp[i - 1, j - 1]);
            }
        }

        return dp[m, n];
    }
}
