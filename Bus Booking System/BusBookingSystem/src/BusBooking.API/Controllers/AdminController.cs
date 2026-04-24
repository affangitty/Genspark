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
        decimal totalRefunded = 0m;
        foreach (var bus in op.Buses)
        {
            var futureBookings = await _unitOfWork.Bookings.GetFutureBookingsByBusIdAsync(bus.Id);
            foreach (var booking in futureBookings)
            {
                if (booking.Cancellation != null)
                    continue;

                booking.Status = Domain.Enums.BookingStatus.CancelledByAdmin;
                _unitOfWork.Bookings.Update(booking);
                futureCancelledCount++;
                var refundAmount = booking.TotalAmount;
                totalRefunded += refundAmount;

                var cancellation = new BusBooking.Domain.Entities.Cancellation
                {
                    BookingId = booking.Id,
                    CancelledAt = DateTime.UtcNow,
                    Reason = $"Cancelled by admin due to operator disablement: {request.Reason}",
                    RefundPercentage = 100m,
                    RefundAmount = refundAmount,
                    IsAdminInitiated = true
                };
                await _unitOfWork.Bookings.AddCancellationAsync(cancellation);

                if (booking.Payment != null)
                {
                    booking.Payment.RefundAmount = refundAmount;
                    booking.Payment.RefundedAt = DateTime.UtcNow;
                    booking.Payment.Status = PaymentStatus.Refunded;
                }

                var passengerSeatIds = booking.Passengers.Select(p => p.SeatId).ToList();
                await _unitOfWork.SeatLocks.ReleaseLocksForSeatsAsync(passengerSeatIds, booking.JourneyDate.Date);

                // Notify user of cancellation + refund
                await _mailService.SendCancellationNoticeAsync(
                    booking.User.Email,
                    booking.BookingReference,
                    refundAmount);
            }
        }

        await _mailService.SendOperatorDisabledNoticeAsync(op.Email, op.CompanyName, request.Reason);

        _unitOfWork.BusOperators.Update(op);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new
        {
            message = "Operator disabled successfully.",
            bookingsCancelled = futureCancelledCount,
            totalRefunded
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
        var admin = await _unitOfWork.Users.GetByEmailAsync(request.ResolveLoginId().ToLowerInvariant());

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
        // Never return 404 here: the admin SPA loads this in forkJoin with approvals; missing row would hide the whole dashboard including pending operators.
        if (config == null)
            return Ok(new BusBooking.Application.DTOs.Admin.PlatformConfigDto
            {
                ConvenienceFeePercentage = 5,
                UseFlatConvenienceFee = false,
                FlatConvenienceFeePerPassenger = 0,
                SeatLockDurationMinutes = 10
            });

        return Ok(new BusBooking.Application.DTOs.Admin.PlatformConfigDto
        {
            ConvenienceFeePercentage = config.ConvenienceFeePercentage,
            UseFlatConvenienceFee = config.UseFlatConvenienceFee,
            FlatConvenienceFeePerPassenger = config.FlatConvenienceFeePerPassenger,
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
        if (request.ConvenienceFeePercentage < 0 || request.ConvenienceFeePercentage > 100)
            return BadRequest(new { message = "Convenience fee percentage must be between 0 and 100." });

        if (request.SeatLockDurationMinutes < 1 || request.SeatLockDurationMinutes > 120)
            return BadRequest(new { message = "Seat lock duration must be between 1 and 120 minutes." });

        var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");
        if (adminIdClaim == null) return Unauthorized();

        var config = await _unitOfWork.PlatformConfig.GetCurrentAsync();
        if (config == null)
            return NotFound(new { message = "Platform config not found." });

        config.ConvenienceFeePercentage = request.ConvenienceFeePercentage;
        config.UseFlatConvenienceFee = request.UseFlatConvenienceFee;
        config.FlatConvenienceFeePerPassenger = request.FlatConvenienceFeePerPassenger;
        config.SeatLockDurationMinutes = request.SeatLockDurationMinutes;
        config.UpdatedByAdminId = adminIdClaim.Value;

        _unitOfWork.PlatformConfig.Update(config);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Platform config updated successfully." });
    }

    /// <summary>
    /// Admin revenue dashboard with overall and per-operator stats
    /// </summary>
    [HttpGet("revenue-dashboard")]
    public async Task<IActionResult> GetRevenueDashboard()
    {
        var operators = await _unitOfWork.BusOperators.GetAllAsync();
        var operatorRevenue = new List<AdminOperatorRevenueDto>();

        foreach (var op in operators)
        {
            var bookings = (await _unitOfWork.Bookings.GetByOperatorIdAsync(op.Id)).ToList();
            var confirmedBookings = bookings.Count(b => b.Status == BookingStatus.Confirmed);
            var cancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.CancelledByAdmin);
            var grossRevenue = bookings
                .Where(b => b.Status == BookingStatus.Confirmed)
                .Sum(b => b.TotalAmount);
            var totalRefunds = bookings.Sum(b => b.Cancellation?.RefundAmount ?? 0m);

            operatorRevenue.Add(new AdminOperatorRevenueDto
            {
                OperatorId = op.Id,
                OperatorName = op.CompanyName,
                TotalBookings = bookings.Count,
                ConfirmedBookings = confirmedBookings,
                CancelledBookings = cancelledBookings,
                GrossRevenue = grossRevenue,
                TotalRefunds = totalRefunds,
                NetRevenue = grossRevenue - totalRefunds
            });
        }

        var dashboard = new AdminRevenueDashboardDto
        {
            TotalGrossRevenue = operatorRevenue.Sum(o => o.GrossRevenue),
            TotalRefunds = operatorRevenue.Sum(o => o.TotalRefunds),
            TotalBookings = operatorRevenue.Sum(o => o.TotalBookings),
            ConfirmedBookings = operatorRevenue.Sum(o => o.ConfirmedBookings),
            CancelledBookings = operatorRevenue.Sum(o => o.CancelledBookings),
            OperatorRevenue = operatorRevenue.OrderByDescending(o => o.NetRevenue).ToList()
        };
        dashboard.TotalNetRevenue = dashboard.TotalGrossRevenue - dashboard.TotalRefunds;

        return Ok(dashboard);
    }

    /// <summary>
    /// Unified approval queue for pending operators and buses
    /// </summary>
    [HttpGet("approvals/queue")]
    public async Task<IActionResult> GetUnifiedApprovalQueue()
    {
        var pendingOperators = await _unitOfWork.BusOperators.GetByStatusAsync(OperatorStatus.Pending);
        var pendingBuses = await _unitOfWork.Buses.GetByStatusAsync(BusStatus.PendingApproval);
        var pendingRouteAssignments = (await _unitOfWork.BusRouteAssignments.GetPendingAsync()).ToList();

        var operatorItems = pendingOperators.Select(o => new ApprovalQueueItemDto
        {
            Type = "Operator",
            Id = o.Id,
            DisplayName = o.CompanyName,
            RequestedBy = o.ContactPersonName,
            RequestedAt = o.CreatedAt,
            Status = o.Status.ToString(),
            AdditionalContext = o.Email
        });

        var busItems = pendingBuses.Select(b => new ApprovalQueueItemDto
        {
            Type = "Bus",
            Id = b.Id,
            DisplayName = $"{b.BusNumber} - {b.BusName}",
            RequestedBy = b.Operator?.CompanyName ?? "Unknown Operator",
            RequestedAt = b.CreatedAt,
            Status = b.Status.ToString(),
            AdditionalContext = b.Route != null
                ? $"{b.Route.SourceCity} -> {b.Route.DestinationCity}"
                : "Route not assigned"
        });

        var routeAssignmentItems = pendingRouteAssignments.Select(a => new ApprovalQueueItemDto
        {
            Type = "RouteAssignment",
            Id = a.Id,
            DisplayName = $"{a.Bus.BusNumber} / {a.Bus.BusName} → {a.Route.SourceCity} – {a.Route.DestinationCity}",
            RequestedBy = a.Bus.Operator?.CompanyName ?? "Unknown Operator",
            RequestedAt = a.CreatedAt,
            Status = "Pending",
            AdditionalContext =
                "Dep " + a.DepartureTime.ToString(@"hh\:mm")
                + ", Arr " + a.ArrivalTime.ToString(@"hh\:mm")
                + ", ₹" + a.BaseFare.ToString("F0")
        });

        var queue = operatorItems
            .Concat(busItems)
            .Concat(routeAssignmentItems)
            .OrderBy(item => item.RequestedAt)
            .ToList();

        return Ok(new
        {
            totalPending = queue.Count,
            pendingOperators = operatorItems.Count(),
            pendingBuses = busItems.Count(),
            pendingRouteAssignments = routeAssignmentItems.Count(),
            items = queue
        });
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