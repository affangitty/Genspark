using BusBooking.Application.DTOs.Admin;
using BusBooking.Application.Interfaces;
using BusBooking.Domain.Enums;
using BusBooking.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMailService _mailService;

    public AdminController(IUnitOfWork unitOfWork, IMailService mailService)
    {
        _unitOfWork = unitOfWork;
        _mailService = mailService;
    }

    /// <summary>
    /// Get all pending operator registrations
    /// </summary>
    [HttpGet("operators/pending")]
    public async Task<IActionResult> GetPendingOperators()
    {
        var operators = await _unitOfWork.BusOperators.GetByStatusAsync(OperatorStatus.Pending);
        return Ok(operators.Select(o => new OperatorApprovalDto
        {
            Id = o.Id,
            CompanyName = o.CompanyName,
            ContactPersonName = o.ContactPersonName,
            Email = o.Email,
            PhoneNumber = o.PhoneNumber,
            Status = o.Status.ToString(),
            CreatedAt = o.CreatedAt
        }));
    }

    /// <summary>
    /// Get all operators
    /// </summary>
    [HttpGet("operators")]
    public async Task<IActionResult> GetAllOperators()
    {
        var operators = await _unitOfWork.BusOperators.GetAllAsync();
        return Ok(operators.Select(o => new OperatorApprovalDto
        {
            Id = o.Id,
            CompanyName = o.CompanyName,
            ContactPersonName = o.ContactPersonName,
            Email = o.Email,
            PhoneNumber = o.PhoneNumber,
            Status = o.Status.ToString(),
            CreatedAt = o.CreatedAt
        }));
    }

    /// <summary>
    /// Approve or reject a bus operator
    /// </summary>
    [HttpPost("operators/{operatorId}/approve")]
    public async Task<IActionResult> ApproveOperator(
        Guid operatorId, [FromBody] ApproveOperatorRequestDto request)
    {
        var op = await _unitOfWork.BusOperators.GetByIdAsync(operatorId);
        if (op == null)
            return NotFound(new { message = "Operator not found." });

        if (op.Status != OperatorStatus.Pending)
            return BadRequest(new { message = "Operator is not in pending status." });

        if (request.IsApproved)
        {
            op.Status = OperatorStatus.Approved;
            op.ApprovedAt = DateTime.UtcNow;
            await _mailService.SendEmailAsync(
                op.Email,
                "Bus Booking — Registration Approved",
                $"Dear {op.CompanyName}, your operator registration has been approved. You can now log in.");
        }
        else
        {
            op.Status = OperatorStatus.Rejected;
            op.RejectionReason = request.RejectionReason;
            await _mailService.SendEmailAsync(
                op.Email,
                "Bus Booking — Registration Rejected",
                $"Dear {op.CompanyName}, your registration was rejected. Reason: {request.RejectionReason}");
        }

        _unitOfWork.BusOperators.Update(op);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new
        {
            message = request.IsApproved ? "Operator approved." : "Operator rejected.",
            operatorId = op.Id,
            status = op.Status.ToString()
        });
    }

    /// <summary>
    /// Disable an approved operator — cancels all future bookings
    /// </summary>
    [HttpPost("operators/{operatorId}/disable")]
    public async Task<IActionResult> DisableOperator(
        Guid operatorId, [FromBody] DisableOperatorRequestDto request)
    {
        var op = await _unitOfWork.BusOperators.GetByIdAsync(operatorId);
        if (op == null)
            return NotFound(new { message = "Operator not found." });

        if (op.Status == OperatorStatus.Disabled)
            return BadRequest(new { message = "Operator is already disabled." });

        op.Status = OperatorStatus.Disabled;
        op.DisabledAt = DateTime.UtcNow;
        op.AdminNotes = request.Reason;

        // Cancel all future bookings on operator's buses
        var futureCancelledCount = 0;
        foreach (var bus in op.Buses)
        {
            var futureBookings = await _unitOfWork.Bookings.GetFutureBookingsByBusIdAsync(bus.Id);
            foreach (var booking in futureBookings)
            {
                booking.Status = Domain.Enums.BookingStatus.CancelledByAdmin;
                _unitOfWork.Bookings.Update(booking);
                futureCancelledCount++;

                // Notify user
                await _mailService.SendOperatorDisabledNoticeAsync(
                    booking.User.Email,
                    op.CompanyName);
            }
        }

        // Notify operator
        await _mailService.SendEmailAsync(
            op.Email,
            "Bus Booking — Account Disabled",
            $"Your operator account has been disabled. Reason: {request.Reason}");

        _unitOfWork.BusOperators.Update(op);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new
        {
            message = "Operator disabled successfully.",
            bookingsCancelled = futureCancelledCount
        });
    }

    /// <summary>
    /// Re-enable a disabled operator
    /// </summary>
    [HttpPost("operators/{operatorId}/enable")]
    public async Task<IActionResult> EnableOperator(Guid operatorId)
    {
        var op = await _unitOfWork.BusOperators.GetByIdAsync(operatorId);
        if (op == null)
            return NotFound(new { message = "Operator not found." });

        if (op.Status != OperatorStatus.Disabled)
            return BadRequest(new { message = "Operator is not disabled." });

        op.Status = OperatorStatus.Approved;
        op.DisabledAt = null;

        await _mailService.SendEmailAsync(
            op.Email,
            "Bus Booking — Account Re-enabled",
            $"Dear {op.CompanyName}, your operator account has been re-enabled.");

        _unitOfWork.BusOperators.Update(op);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Operator re-enabled successfully." });
    }

    /// <summary>
    /// Admin login
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] BusBooking.Application.DTOs.Auth.LoginRequestDto request,
        [FromServices] BusBooking.Application.Interfaces.IJwtService jwtService)
    {
        var admin = await _unitOfWork.Users.GetByEmailAsync(request.Email.ToLower().Trim());

        if (admin == null || admin.Role != UserRole.Admin)
            return Unauthorized(new { message = "Invalid credentials." });

        if (!BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials." });

        if (!admin.IsActive)
            return Unauthorized(new { message = "Account is deactivated." });

        var (accessToken, refreshToken) = jwtService.GenerateTokenPair(
            admin.Id, admin.Email, "Admin");

        return Ok(new BusBooking.Application.DTOs.Auth.LoginResponseDto
        {
            UserId = admin.Id,
            Email = admin.Email,
            FullName = admin.FullName,
            Role = "Admin",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        });
    }

    /// <summary>
    /// Get platform config
    /// </summary>
    [HttpGet("platform-config")]
    public async Task<IActionResult> GetPlatformConfig()
    {
        var config = await _unitOfWork.PlatformConfig.GetCurrentAsync();
        if (config == null)
            return NotFound(new { message = "Platform config not found." });

        return Ok(new BusBooking.Application.DTOs.Admin.PlatformConfigDto
        {
            ConvenienceFeePercentage = config.ConvenienceFeePercentage,
            SeatLockDurationMinutes = config.SeatLockDurationMinutes
        });
    }

    /// <summary>
    /// Update platform convenience fee and seat lock duration
    /// </summary>
    [HttpPut("platform-config")]
    public async Task<IActionResult> UpdatePlatformConfig(
        [FromBody] BusBooking.Application.DTOs.Admin.PlatformConfigDto request)
    {
        var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");
        if (adminIdClaim == null) return Unauthorized();

        var config = await _unitOfWork.PlatformConfig.GetCurrentAsync();
        if (config == null)
            return NotFound(new { message = "Platform config not found." });

        config.ConvenienceFeePercentage = request.ConvenienceFeePercentage;
        config.SeatLockDurationMinutes = request.SeatLockDurationMinutes;
        config.UpdatedByAdminId = adminIdClaim.Value;

        _unitOfWork.PlatformConfig.Update(config);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Platform config updated successfully." });
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return Ok(users.Where(u => u.Role == UserRole.User)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.IsActive,
                u.CreatedAt
            }));
    }
}