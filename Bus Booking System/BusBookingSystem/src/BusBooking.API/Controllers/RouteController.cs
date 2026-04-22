using BusBooking.Application.DTOs.Route;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Route = BusBooking.Domain.Entities.Route;

namespace BusBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RouteController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public RouteController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Get all active routes — public, no login required
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllRoutes()
    {
        var routes = await _unitOfWork.Routes.GetAllActiveAsync();
        return Ok(routes.Select(r => new RouteResponseDto
        {
            Id = r.Id,
            SourceCity = r.SourceCity,
            DestinationCity = r.DestinationCity,
            SourceState = r.SourceState,
            DestinationState = r.DestinationState,
            IsActive = r.IsActive
        }));
    }

    /// <summary>
    /// Get all routes including inactive — admin only
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllRoutesAdmin()
    {
        var routes = await _unitOfWork.Routes.GetAllAsync();
        return Ok(routes.Select(r => new RouteResponseDto
        {
            Id = r.Id,
            SourceCity = r.SourceCity,
            DestinationCity = r.DestinationCity,
            SourceState = r.SourceState,
            DestinationState = r.DestinationState,
            IsActive = r.IsActive
        }));
    }

    /// <summary>
    /// Get route by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRouteById(Guid id)
    {
        var route = await _unitOfWork.Routes.GetByIdAsync(id);
        if (route == null)
            return NotFound(new { message = "Route not found." });

        return Ok(new RouteResponseDto
        {
            Id = route.Id,
            SourceCity = route.SourceCity,
            DestinationCity = route.DestinationCity,
            SourceState = route.SourceState,
            DestinationState = route.DestinationState,
            IsActive = route.IsActive
        });
    }

    /// <summary>
    /// Get all unique source cities — for search autocomplete
    /// </summary>
    [HttpGet("cities/source")]
    public async Task<IActionResult> GetSourceCities()
    {
        var routes = await _unitOfWork.Routes.GetAllActiveAsync();
        var cities = routes
            .Select(r => new { r.SourceCity, r.SourceState })
            .DistinctBy(c => c.SourceCity)
            .OrderBy(c => c.SourceCity)
            .ToList();

        return Ok(cities);
    }

    /// <summary>
    /// Get destination cities for a given source — for search autocomplete
    /// </summary>
    [HttpGet("cities/destination")]
    public async Task<IActionResult> GetDestinationCities([FromQuery] string sourceCity)
    {
        if (string.IsNullOrWhiteSpace(sourceCity))
            return BadRequest(new { message = "Source city is required." });

        var routes = await _unitOfWork.Routes.GetAllActiveAsync();
        var destinations = routes
            .Where(r => r.SourceCity.ToLower() == sourceCity.ToLower())
            .Select(r => new { r.DestinationCity, r.DestinationState })
            .DistinctBy(c => c.DestinationCity)
            .OrderBy(c => c.DestinationCity)
            .ToList();

        return Ok(destinations);
    }

    /// <summary>
    /// Create a new route — admin only
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRoute([FromBody] CreateRouteRequestDto request)
    {
        // Check for duplicate route
        var existing = await _unitOfWork.Routes.GetBySourceDestinationAsync(
            request.SourceCity, request.DestinationCity);

        if (existing != null)
            return Conflict(new { message = "A route between these cities already exists." });

        var route = new Route
        {
            SourceCity = request.SourceCity.Trim(),
            DestinationCity = request.DestinationCity.Trim(),
            SourceState = request.SourceState.Trim(),
            DestinationState = request.DestinationState.Trim(),
            IsActive = true
        };

        await _unitOfWork.Routes.AddAsync(route);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRouteById), new { id = route.Id },
            new RouteResponseDto
            {
                Id = route.Id,
                SourceCity = route.SourceCity,
                DestinationCity = route.DestinationCity,
                SourceState = route.SourceState,
                DestinationState = route.DestinationState,
                IsActive = route.IsActive
            });
    }

    /// <summary>
    /// Update a route — admin only
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRoute(Guid id, [FromBody] CreateRouteRequestDto request)
    {
        var route = await _unitOfWork.Routes.GetByIdAsync(id);
        if (route == null)
            return NotFound(new { message = "Route not found." });

        // Check duplicate only if source/destination changed
        if (!route.SourceCity.Equals(request.SourceCity, StringComparison.OrdinalIgnoreCase) ||
            !route.DestinationCity.Equals(request.DestinationCity, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _unitOfWork.Routes.GetBySourceDestinationAsync(
                request.SourceCity, request.DestinationCity);
            if (existing != null)
                return Conflict(new { message = "A route between these cities already exists." });
        }

        route.SourceCity = request.SourceCity.Trim();
        route.DestinationCity = request.DestinationCity.Trim();
        route.SourceState = request.SourceState.Trim();
        route.DestinationState = request.DestinationState.Trim();

        _unitOfWork.Routes.Update(route);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Route updated successfully." });
    }

    /// <summary>
    /// Deactivate a route — admin only
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateRoute(Guid id)
    {
        var route = await _unitOfWork.Routes.GetByIdAsync(id);
        if (route == null)
            return NotFound(new { message = "Route not found." });

        if (!route.IsActive)
            return BadRequest(new { message = "Route is already inactive." });

        route.IsActive = false;
        _unitOfWork.Routes.Update(route);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Route deactivated successfully." });
    }

    /// <summary>
    /// Reactivate a route — admin only
    /// </summary>
    [HttpPost("{id}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActivateRoute(Guid id)
    {
        var route = await _unitOfWork.Routes.GetByIdAsync(id);
        if (route == null)
            return NotFound(new { message = "Route not found." });

        if (route.IsActive)
            return BadRequest(new { message = "Route is already active." });

        route.IsActive = true;
        _unitOfWork.Routes.Update(route);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Route activated successfully." });
    }
}