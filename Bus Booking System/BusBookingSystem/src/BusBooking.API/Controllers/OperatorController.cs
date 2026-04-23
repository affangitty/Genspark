using BusBooking.Application.DTOs.Auth;
using BusBooking.Application.Interfaces;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using BusBooking.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OperatorController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;

    public OperatorController(IUnitOfWork unitOfWork, IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Register as a bus operator — requires admin approval before login
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] OperatorRegistrationRequestDto request)
    {
        var email = request.Email.ToLower().Trim();

        var existing = await _unitOfWork.BusOperators.GetByEmailAsync(email);
        if (existing != null)
            return Conflict(new { message = "Email is already registered." });

        if (request.Password != request.ConfirmPassword)
            return BadRequest(new { message = "Passwords do not match." });

        var op = new BusOperator
        {
            CompanyName = request.CompanyName,
            ContactPersonName = request.ContactPersonName,
            Email = email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = OperatorStatus.Pending
        };

        await _unitOfWork.BusOperators.AddAsync(op);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProfile), null, new
        {
            message = "Registration submitted. Awaiting admin approval.",
            operatorId = op.Id
        });
    }

    /// <summary>
    /// Login for approved bus operators
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var op = await _unitOfWork.BusOperators.GetByEmailAsync(request.Email.ToLower().Trim());

        if (op == null || !BCrypt.Net.BCrypt.Verify(request.Password, op.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        if (op.Status == OperatorStatus.Pending)
            return Unauthorized(new { message = "Your account is pending admin approval." });

        if (op.Status == OperatorStatus.Rejected)
            return Unauthorized(new { message = $"Your registration was rejected. Reason: {op.RejectionReason}" });

        if (op.Status == OperatorStatus.Disabled)
            return Unauthorized(new { message = "Your account has been disabled by admin." });

        var (accessToken, refreshToken) = _jwtService.GenerateTokenPair(
            op.Id, op.Email, "Operator");

        return Ok(new LoginResponseDto
        {
            UserId = op.Id,
            Email = op.Email,
            FullName = op.CompanyName,
            Role = "Operator",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        });
    }

    /// <summary>
    /// Get operator profile
    /// </summary>
    [HttpGet("profile")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetProfile()
    {
        var operatorId = GetOperatorId();
        if (operatorId == null) return Unauthorized();

        var op = await _unitOfWork.BusOperators.GetByIdAsync(operatorId.Value);
        if (op == null) return NotFound();

        return Ok(new
        {
            op.Id,
            op.CompanyName,
            op.ContactPersonName,
            op.Email,
            op.PhoneNumber,
            Status = op.Status.ToString(),
            op.ApprovedAt,
            op.CreatedAt,
            Locations = op.Locations.Select(l => new
            {
                l.Id,
                l.City,
                l.AddressLine,
                l.Landmark,
                l.State,
                l.PinCode
            })
        });
    }

    /// <summary>
    /// Update operator profile
    /// </summary>
    [HttpPut("profile")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateOperatorProfileRequestDto request)
    {
        var operatorId = GetOperatorId();
        if (operatorId == null) return Unauthorized();

        var op = await _unitOfWork.BusOperators.GetByIdAsync(operatorId.Value);
        if (op == null) return NotFound();

        op.ContactPersonName = request.ContactPersonName;
        op.PhoneNumber = request.PhoneNumber;

        _unitOfWork.BusOperators.Update(op);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Profile updated successfully." });
    }

    /// <summary>
    /// Add an office location for the operator
    /// </summary>
    [HttpPost("locations")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> AddLocation([FromBody] AddOperatorLocationRequestDto request)
    {
        var operatorId = GetOperatorId();
        if (operatorId == null) return Unauthorized();

        var op = await _unitOfWork.BusOperators.GetByIdAsync(operatorId.Value);
        if (op == null) return NotFound();

        var location = new OperatorLocation
        {
            OperatorId = operatorId.Value,
            City = request.City,
            AddressLine = request.AddressLine,
            Landmark = request.Landmark,
            State = request.State,
            PinCode = request.PinCode
        };

        await _unitOfWork.BusOperators.AddLocationAsync(location);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProfile), null, new
        {
            message = "Location added successfully.",
            locationId = location.Id
        });
    }

    /// <summary>
    /// Get all locations for the operator
    /// </summary>
    [HttpGet("locations")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetLocations()
    {
        var operatorId = GetOperatorId();
        if (operatorId == null) return Unauthorized();

        var locations = await _unitOfWork.BusOperators.GetLocationsByOperatorIdAsync(operatorId.Value);

        return Ok(locations.Select(l => new
        {
            l.Id,
            l.City,
            l.AddressLine,
            l.Landmark,
            l.State,
            l.PinCode
        }));
    }

    /// <summary>
    /// Delete an operator location
    /// </summary>
    [HttpDelete("locations/{locationId}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> DeleteLocation(Guid locationId)
    {
        var operatorId = GetOperatorId();
        if (operatorId == null) return Unauthorized();

        var location = await _unitOfWork.BusOperators.GetLocationByIdAsync(locationId);
        if (location == null || location.OperatorId != operatorId.Value)
            return NotFound(new { message = "Location not found." });

        _unitOfWork.BusOperators.RemoveLocation(location);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Location removed successfully." });
    }

    private Guid? GetOperatorId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");
        return claim != null ? Guid.Parse(claim.Value) : null;
    }
}