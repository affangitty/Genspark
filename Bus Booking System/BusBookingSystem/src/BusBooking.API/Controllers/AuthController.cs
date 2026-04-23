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
public class AuthController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;

    public AuthController(IUnitOfWork unitOfWork, IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var email = request.Email.ToLower().Trim();

        // Must match stored email (lowercase); otherwise mixed-case re-register bypasses this and hits DB unique index → 500
        var existingUser = await _unitOfWork.Users.GetByEmailAsync(email);
        if (existingUser != null)
            return Conflict(new { message = "Email is already registered." });

        // Check passwords match
        if (request.Password != request.ConfirmPassword)
            return BadRequest(new { message = "Passwords do not match." });

        var user = new User
        {
            FullName = request.FullName,
            Email = email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.User,
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProfile), null, new
        {
            message = "Registration successful.",
            userId = user.Id,
            email = user.Email
        });
    }

    /// <summary>
    /// Login for users
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email.ToLower().Trim());

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        if (!user.IsActive)
            return Unauthorized(new { message = "Your account has been deactivated." });

        var (accessToken, refreshToken) = _jwtService.GenerateTokenPair(
            user.Id, user.Email, user.Role.ToString());

        return Ok(new LoginResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        });
    }

    /// <summary>
    /// Get current logged-in user profile
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");

        if (userIdClaim == null)
            return Unauthorized();

        var userId = Guid.Parse(userIdClaim.Value);
        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            Role = user.Role.ToString(),
            user.IsActive,
            user.CreatedAt
        });
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    [HttpPut("profile")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");

        if (userIdClaim == null)
            return Unauthorized();

        var userId = Guid.Parse(userIdClaim.Value);
        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Profile updated successfully." });
    }

    /// <summary>
    /// Change password for logged-in user
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");

        if (userIdClaim == null)
            return Unauthorized();

        var userId = Guid.Parse(userIdClaim.Value);
        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        if (user == null)
            return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        if (request.NewPassword != request.ConfirmNewPassword)
            return BadRequest(new { message = "New passwords do not match." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Password changed successfully." });
    }

    /// <summary>Exchange a refresh JWT for a new access + refresh token pair.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new ProblemDetails { Title = "Invalid request", Detail = "Refresh token is required." });

        var parsed = _jwtService.ParseRefreshToken(request.RefreshToken.Trim());
        if (parsed == null)
            return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "Invalid or expired refresh token." });

        var (userId, email, role) = parsed.Value;

        if (role == "User" || role == "Admin")
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null || user.Email != email)
                return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "User no longer valid." });
            if (!user.IsActive)
                return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "Account is deactivated." });

            var pair = _jwtService.GenerateTokenPair(user.Id, user.Email, user.Role.ToString());
            return Ok(new LoginResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                AccessToken = pair.accessToken,
                RefreshToken = pair.refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            });
        }

        if (role == "Operator")
        {
            var op = await _unitOfWork.BusOperators.GetByIdAsync(userId);
            if (op == null || op.Email != email)
                return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "Operator no longer valid." });
            if (op.Status != OperatorStatus.Approved)
                return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "Operator account is not approved." });

            var pair = _jwtService.GenerateTokenPair(op.Id, op.Email, "Operator");
            return Ok(new LoginResponseDto
            {
                UserId = op.Id,
                Email = op.Email,
                FullName = op.CompanyName,
                Role = "Operator",
                AccessToken = pair.accessToken,
                RefreshToken = pair.refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            });
        }

        return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "Unknown role in refresh token." });
    }
}