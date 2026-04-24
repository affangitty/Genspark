using BusBooking.Application.Common;
using BusBooking.Application.DTOs.Bus;
using BusBooking.Domain;
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
        // UTC calendar day for timestamptz columns and Npgsql parameter rules.
        var journeyDate = JourneyDateUtc.ToUtcCalendarStart(request.JourneyDate);

        var allRoutes = await _unitOfWork.Routes.GetAllActiveAsync();
        
        // Fuzzy match on source and destination
        var matchedRoutes = allRoutes.Where(r =>
            FuzzyMatch(r.SourceCity, request.SourceCity) &&
            FuzzyMatch(r.DestinationCity, request.DestinationCity)
        ).ToList();

        var result = new List<BusResponseDto>();
        var platformConfig = await _unitOfWork.PlatformConfig.GetCurrentAsync();

        foreach (var route in matchedRoutes)
        {
            // Get buses assigned to this route for the given date
            var buses = await _unitOfWork.Buses.GetByRouteIdAsync(route.Id);
            
            foreach (var bus in buses)
            {
                if (bus.Operator == null || bus.Seats == null)
                    continue;

                if (bus.Status != BusStatus.Active || bus.Operator.Status != OperatorStatus.Approved)
                    continue;

                // Operator must have office locations in both source and destination cities
                // so we can show boarding + drop-off, and so booking doesn't fail later with 409.
                var (boardingAddress, dropOffAddress) = await ResolveBoardingAddressesAsync(bus);
                if (boardingAddress == null || dropOffAddress == null)
                    continue;

                var bookedSeatIds = await _unitOfWork.Bookings.GetBookedSeatIdsByBusAndDateAsync(bus.Id, journeyDate);
                var lockedSeatIds = await _unitOfWork.SeatLocks.GetActiveLockSeatIdsByBusAndDateAsync(
                    bus.Id, journeyDate, exceptUserId: null);
                var totalSeats = bus.Seats.Count(s => s.IsActive);
                // If seats were not generated for this bus yet, do not surface it in search.
                if (totalSeats <= 0)
                    continue;
                var unavailable = bookedSeatIds.Concat(lockedSeatIds).ToHashSet();
                var availableSeats = totalSeats - unavailable.Count;

                // Skip if not enough seats available for requested passengers
                if (request.PassengerCount.HasValue && availableSeats < request.PassengerCount)
                    continue;

                var convenienceFee = PlatformFeeCalculator.ComputeConvenienceForSingleSeat(platformConfig, bus.BaseFare);

                var busDto = new BusResponseDto
                {
                    Id = bus.Id,
                    BusNumber = bus.BusNumber,
                    BusName = bus.BusName,
                    OperatorName = bus.Operator.CompanyName,
                    SourceCity = route.SourceCity,
                    DestinationCity = route.DestinationCity,
                    BoardingAddress = boardingAddress,
                    DropOffAddress = dropOffAddress,
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

        source = CanonicalCityName(NormalizeName(source));
        target = CanonicalCityName(NormalizeName(target));

        if (source == target)
            return true;

        // Substring match for longer names (e.g. "north mumbai" vs "mumbai")
        if (source.Length >= 4 && target.Length >= 4)
        {
            if (source.Contains(target, StringComparison.Ordinal) || target.Contains(source, StringComparison.Ordinal))
                return true;
        }

        // Levenshtein distance based matching
        var distance = LevenshteinDistance(source, target);
        var maxLength = Math.Max(source.Length, target.Length);
        var similarity = 1.0 - (double)distance / maxLength;

        return similarity >= threshold;
    }

    /// <summary>
    /// Map common alternate spellings so route rows match user input (e.g. Bengaluru vs Bangalore).
    /// </summary>
    private static string CanonicalCityName(string normalizedLower)
    {
        return normalizedLower switch
        {
            "bengaluru" or "blr" => "bangalore",
            "bombay" => "mumbai",
            "calcutta" => "kolkata",
            _ => normalizedLower
        };
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

    private async Task<(string? Boarding, string? DropOff)> ResolveBoardingAddressesAsync(BusBooking.Domain.Entities.Bus bus)
    {
        var locations = (await _unitOfWork.BusOperators.GetLocationsByOperatorIdAsync(bus.OperatorId)).ToList();
        if (locations.Count == 0)
            return (null, null);

        var route = bus.Route;
        if (route == null)
            return (FormatOperatorLocation(locations[0]), FormatOperatorLocation(locations.Count > 1 ? locations[^1] : locations[0]));

        var boarding = locations.FirstOrDefault(l => CityMatchesRouteCity(l.City, route.SourceCity));
        var dropOff = locations.FirstOrDefault(l => CityMatchesRouteCity(l.City, route.DestinationCity));

        return (boarding == null ? null : FormatOperatorLocation(boarding),
            dropOff == null ? null : FormatOperatorLocation(dropOff));
    }

    private static string FormatOperatorLocation(BusBooking.Domain.Entities.OperatorLocation l)
    {
        var parts = new[] { l.AddressLine, l.Landmark, l.City, l.State, l.PinCode }
            .Where(s => !string.IsNullOrWhiteSpace(s));
        return string.Join(", ", parts);
    }

    private static bool CityMatchesRouteCity(string locationCity, string routeCity)
    {
        var a = NormalizeCityToken(locationCity);
        var b = NormalizeCityToken(routeCity);
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return false;
        return a == b || a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal);
    }

    private static string NormalizeCityToken(string name) =>
        Regex.Replace(name.ToLowerInvariant().Trim(), @"\s+", " ");
}
