using BusBooking.Application.DTOs.Bus;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using BusBooking.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public BusController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ── Bus Layout Endpoints ──────────────────────────────────

    /// <summary>
    /// Create a bus layout — operator only
    /// </summary>
    [HttpPost("layouts")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> CreateLayout([FromBody] CreateBusLayoutRequestDto request)
    {
        var operatorId = GetOperatorId();
        if (operatorId == null) return Unauthorized();

        var layout = new BusLayout
        {
            OperatorId = operatorId.Value,
            LayoutName = request.LayoutName,
            TotalSeats = request.TotalSeats,
            Rows = request.Rows,
            Columns = request.Columns,
            HasUpperDeck = request.HasUpperDeck,
            LayoutJson = request.LayoutJson
        };

        await _unitOfWork.BusLayouts.AddAsync(layout);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLayoutById), new { id = layout.Id },
            new BusLayoutDto
            {
                Id = layout.Id,
                LayoutName = layout.LayoutName,
                TotalSeats = layout.TotalSeats,
                Rows = layout.Rows,
                Columns = layout.Columns,
                HasUpperDeck = layout.HasUpperDeck,
                LayoutJson = layout.LayoutJson
            });
    }

    /// <summary>
    /// Get layout by ID
    /// </summary>
    [HttpGet("layouts/{id}")]
    [Authorize]
    public async Task<IActionResult> GetLayoutById(Guid id)
    {
        var layout = await _unitOfWork.BusLayouts.GetByIdAsync(id);
        if (layout == null)
            return NotFound(new { message = "Layout not found." });

        return Ok(new BusLayoutDto
        {
            Id = layout.Id,
            LayoutName = layout.LayoutName,
            TotalSeats = layout.TotalSeats,
            Rows = layout.Rows,
            Columns = layout.Columns,
            HasUpperDeck = layout.HasUpperDeck,
            LayoutJson = layout.LayoutJson
        });
    }

    /// <summary>
    /// Get all layouts for the logged-in operator
    /// </summary>
    [HttpGet("layouts")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetMyLayouts()
    {
        var operatorId = GetOperatorId();
        if (operatorId == null) return Unauthorized();

        var layouts = await _unitOfWork.BusLayouts.GetByOperatorIdAsync(operatorId.Value);
        return Ok(layouts.Select(l => new BusLayoutDto
        {
            Id = l.Id,
            LayoutName = l.LayoutName,
            TotalSeats = l.TotalSeats,
            Rows = l.Rows,
            Columns = l.Columns,
            HasUpperDeck = l.HasUpperDeck,
            LayoutJson = l.LayoutJson
        }));
    }

    // ── Bus Endpoints ─────────────────────────────────────────

    /// <summary>
    /// Add a new bus — operator only, requires admin approval
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> AddBus([FromBody] CreateBusRequestDto request)
    {
        var operatorId = GetOperatorId();
        if (operatorId == null) return Unauthorized();

        // Check operator is approved
        var op = await _unitOfWork.BusOperators.GetByIdAsync(operatorId.Value);
        if (op == null || op.Status != OperatorStatus.Approved)
            return Forbid();

        // Check unique bus number
        var existing = await _unitOfWork.Buses.GetByBusNumberAsync(request.BusNumber);
        if (existing != null)
            return Conflict(new { message = "Bus number already exists." });

        // Validate layout belongs to operator
        var layout = await _unitOfWork.BusLayouts.GetByIdAsync(request.LayoutId);
        if (layout == null || layout.OperatorId != operatorId.Value)
            return BadRequest(new { message = "Invalid layout selected." });

        // Validate route if provided
        if (request.RouteId.HasValue)
        {
            var route = await _unitOfWork.Routes.GetByIdAsync(request.RouteId.Value);
            if (route == null || !route.IsActive)
                return BadRequest(new { message = "Invalid or inactive route." });
        }

        var bus = new Bus
        {
            BusNumber = request.BusNumber.Trim().ToUpper(),
            BusName = request.BusName.Trim(),
            OperatorId = operatorId.Value,
            LayoutId = request.LayoutId,
            RouteId = request.RouteId,
            DepartureTime = request.DepartureTime,
            ArrivalTime = request.ArrivalTime,
            BaseFare = request.BaseFare,
            Status = BusStatus.PendingApproval
        };

        await _unitOfWork.Buses.AddAsync(bus);
        await _unitOfWork.SaveChangesAsync();

        // Auto-generate seats from layout
        await GenerateSeatsForBus(bus.Id, layout);

        return CreatedAtAction(nameof(GetBusById), new { id = bus.Id }, new
        {
            message = "Bus submitted for admin approval.",
            busId = bus.Id,
            busNumber = bus.BusNumber,
            status = bus.Status.ToString()
        });
    }

    /// <summary>
    /// Get bus by ID — public
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBusById(Guid id)
    {
        var bus = await _unitOfWork.Buses.GetByIdAsync(id);
        if (bus == null)
            return NotFound(new { message = "Bus not found." });

        var config = await _unitOfWork.PlatformConfig.GetCurrentAsync();
        var fee = config?.ConvenienceFeePercentage ?? 5m;
        var convenienceFee = Math.Round(bus.BaseFare * fee / 100, 2);

        return Ok(new BusResponseDto
        {
            Id = bus.Id,
            BusNumber = bus.BusNumber,
            BusName = bus.BusName,
            OperatorName = bus.Operator?.CompanyName ?? string.Empty,
            SourceCity = bus.Route?.SourceCity ?? string.Empty,
            DestinationCity = bus.Route?.DestinationCity ?? string.Empty,
            DepartureTime = bus.DepartureTime,
            ArrivalTime = bus.ArrivalTime,
            TotalSeats = bus.Layout?.TotalSeats ?? 0,
            AvailableSeats = bus.Seats.Count(s => s.IsActive),
            BaseFare = bus.BaseFare,
            ConvenienceFee = convenienceFee,
            TotalFare = bus.BaseFare + convenienceFee,
            Status = bus.Status.ToString()
        });
    }

    /// <summary>
    /// Get all buses for logged-in operator
    /// </summary>
    [HttpGet("my-buses")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetMyBuses()
    {
        var operatorId = GetOperatorId();
        if (operatorId == null) return Unauthorized();

        var buses = await _unitOfWork.Buses.GetByOperatorIdAsync(operatorId.Value);
        var config = await _unitOfWork.PlatformConfig.GetCurrentAsync();
        var fee = config?.ConvenienceFeePercentage ?? 5m;

        return Ok(buses.Select(b =>
        {
            var convenienceFee = Math.Round(b.BaseFare * fee / 100, 2);
            return new BusResponseDto
            {
                Id = b.Id,
                BusNumber = b.BusNumber,
                BusName = b.BusName,
                OperatorName = b.Operator?.CompanyName ?? string.Empty,
                SourceCity = b.Route?.SourceCity ?? string.Empty,
                DestinationCity = b.Route?.DestinationCity ?? string.Empty,
                DepartureTime = b.DepartureTime,
                ArrivalTime = b.ArrivalTime,
                TotalSeats = b.Layout?.TotalSeats ?? 0,
                AvailableSeats = b.Seats.Count(s => s.IsActive),
                BaseFare = b.BaseFare,
                ConvenienceFee = convenienceFee,
                TotalFare = b.BaseFare + convenienceFee,
                Status = b.Status.ToString()
            };
        }));
    }

    /// <summary>
    /// Update bus fare — operator only
    /// </summary>
    [HttpPut("{id}/fare")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> UpdateFare(Guid id, [FromBody] UpdateFareRequestDto request)
    {
        var operatorId = GetOperatorId();
        if (operatorId == null) return Unauthorized();

        var bus = await _unitOfWork.Buses.GetByIdAsync(id);
        if (bus == null || bus.OperatorId != operatorId.Value)
            return NotFound(new { message = "Bus not found." });

        bus.BaseFare = request.BaseFare;
        _unitOfWork.Buses.Update(bus);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Fare updated successfully.", baseFare = bus.BaseFare });
    }

    /// <summary>
    /// Mark bus as temporarily unavailable — operator only, no approval needed
    /// </summary>
    [HttpPost("{id}/unavailable")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> MarkUnavailable(Guid id)
    {
        var operatorId = GetOperatorId();
        if (operatorId == null) return Unauthorized();

        var bus = await _unitOfWork.Buses.GetByIdAsync(id);
        if (bus == null || bus.OperatorId != operatorId.Value)
            return NotFound(new { message = "Bus not found." });

        if (bus.Status != BusStatus.Active)
            return BadRequest(new { message = "Only active buses can be marked unavailable." });

        bus.Status = BusStatus.TemporarilyUnavailable;
        _unitOfWork.Buses.Update(bus);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Bus marked as temporarily unavailable." });
    }

    /// <summary>
    /// Restore a temporarily unavailable bus — operator only
    /// </summary>
    [HttpPost("{id}/available")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> MarkAvailable(Guid id)
    {
        var operatorId = GetOperatorId();
        if (operatorId == null) return Unauthorized();

        var bus = await _unitOfWork.Buses.GetByIdAsync(id);
        if (bus == null || bus.OperatorId != operatorId.Value)
            return NotFound(new { message = "Bus not found." });

        if (bus.Status != BusStatus.TemporarilyUnavailable)
            return BadRequest(new { message = "Bus is not temporarily unavailable." });

        bus.Status = BusStatus.Active;
        _unitOfWork.Buses.Update(bus);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Bus is now active." });
    }

    /// <summary>
    /// Permanently remove a bus — operator only, no approval needed
    /// </summary>
    [HttpPost("{id}/remove")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> RemoveBus(Guid id)
    {
        var operatorId = GetOperatorId();
        if (operatorId == null) return Unauthorized();

        var bus = await _unitOfWork.Buses.GetByIdAsync(id);
        if (bus == null || bus.OperatorId != operatorId.Value)
            return NotFound(new { message = "Bus not found." });

        if (bus.Status == BusStatus.Removed)
            return BadRequest(new { message = "Bus is already removed." });

        bus.Status = BusStatus.Removed;
        _unitOfWork.Buses.Update(bus);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Bus permanently removed." });
    }

    /// <summary>
    /// Get seats for a bus on a specific journey date
    /// </summary>
    [HttpGet("{id}/seats")]
    public async Task<IActionResult> GetSeats(Guid id, [FromQuery] DateTime journeyDate)
    {
        var bus = await _unitOfWork.Buses.GetByIdAsync(id);
        if (bus == null)
            return NotFound(new { message = "Bus not found." });

        // Get booked seat IDs for this journey date
        var bookedSeatIds = await _unitOfWork.Bookings
            .GetBookedSeatIdsByBusAndDateAsync(id, journeyDate.Date);

        // Get locked seat IDs
        var lockedSeatIds = await _unitOfWork.SeatLocks
            .GetActiveLockSeatIdsByBusAndDateAsync(id, journeyDate.Date);

        var seats = bus.Seats.Where(s => s.IsActive).Select(s => new BusBooking.Application.DTOs.Booking.SeatDto
        {
            Id = s.Id,
            SeatNumber = s.SeatNumber,
            Row = s.Row,
            Column = s.Column,
            Deck = s.Deck,
            SeatType = s.SeatType.ToString(),
            IsAvailable = !bookedSeatIds.Contains(s.Id) && !lockedSeatIds.Contains(s.Id),
            IsLocked = lockedSeatIds.Contains(s.Id)
        });

        return Ok(seats);
    }

    // ── Admin Bus Approval ────────────────────────────────────

    /// <summary>
    /// Get all buses pending approval — admin only
    /// </summary>
    [HttpGet("pending-approval")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPendingBuses()
    {
        var buses = await _unitOfWork.Buses.GetByStatusAsync(BusStatus.PendingApproval);
        return Ok(buses.Select(b => new
        {
            b.Id,
            b.BusNumber,
            b.BusName,
            OperatorName = b.Operator?.CompanyName,
            OperatorId = b.OperatorId,
            Route = b.Route != null ? $"{b.Route.SourceCity} → {b.Route.DestinationCity}" : "Not assigned",
            b.BaseFare,
            b.CreatedAt
        }));
    }

    /// <summary>
    /// Approve or reject a bus — admin only
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveBus(
        Guid id, [FromBody] BusBooking.Application.DTOs.Admin.ApproveBusRequestDto request)
    {
        var bus = await _unitOfWork.Buses.GetByIdAsync(id);
        if (bus == null)
            return NotFound(new { message = "Bus not found." });

        if (bus.Status != BusStatus.PendingApproval)
            return BadRequest(new { message = "Bus is not pending approval." });

        if (request.IsApproved)
        {
            bus.Status = BusStatus.Active;
            bus.ApprovedAt = DateTime.UtcNow;
            bus.AdminNotes = request.AdminNotes;
        }
        else
        {
            bus.Status = BusStatus.Removed;
            bus.AdminNotes = request.AdminNotes;
        }

        _unitOfWork.Buses.Update(bus);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new
        {
            message = request.IsApproved ? "Bus approved." : "Bus rejected.",
            busId = bus.Id,
            status = bus.Status.ToString()
        });
    }

    // ── Helpers ───────────────────────────────────────────────

    private Guid? GetOperatorId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");
        return claim != null ? Guid.Parse(claim.Value) : null;
    }

    private async Task GenerateSeatsForBus(Guid busId, BusLayout layout)
    {
        try
        {
            var seatDefinitions = System.Text.Json.JsonSerializer
                .Deserialize<List<SeatDefinition>>(layout.LayoutJson);

            if (seatDefinitions == null || seatDefinitions.Count == 0)
                return;

            var seats = seatDefinitions.Select(sd => new Seat
            {
                BusId = busId,
                SeatNumber = sd.SeatNumber,
                Row = sd.Row,
                Column = sd.Column,
                Deck = sd.Deck,
                SeatType = Enum.TryParse<SeatType>(sd.Type, out var st) ? st : SeatType.Seater,
                IsActive = true
            }).ToList();

            await _unitOfWork.Seats.AddRangeAsync(seats);
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            // Layout JSON malformed — seats not generated, handled gracefully
        }
    }

    private class SeatDefinition
    {
        public string SeatNumber { get; set; } = string.Empty;
        public int Row { get; set; }
        public int Column { get; set; }
        public string Deck { get; set; } = "lower";
        public string Type { get; set; } = "Seater";
    }
}