using System.Security.Claims;
using BusBooking.Application.DTOs.Bus;
using BusBooking.Application.Features.Search.Queries;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using BusBooking.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public BusController(IUnitOfWork unitOfWork, IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    // ── Bus Search Endpoint ───────────────────────────────────

    /// <summary>
    /// Search available buses with fuzzy matching on location names
    /// Returns buses with available seat counts and pricing
    /// </summary>
    [HttpPost("search")]
    [AllowAnonymous]
    public async Task<ActionResult<List<BusResponseDto>>> SearchBuses(
        [FromBody] BusSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new SearchBusesQuery
            {
                SourceCity = request.SourceCity,
                DestinationCity = request.DestinationCity,
                JourneyDate = request.JourneyDate,
                PassengerCount = request.PassengerCount
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error searching buses", error = ex.Message });
        }
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

        var bookedSeatIds = await _unitOfWork.Bookings
            .GetBookedSeatIdsByBusAndDateAsync(id, journeyDate.Date);

        Guid? viewerId = null;
        var sub = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (sub != null && Guid.TryParse(sub.Value, out var uid))
            viewerId = uid;

        var lockedByOthers = await _unitOfWork.SeatLocks
            .GetActiveLockSeatIdsByBusAndDateAsync(id, journeyDate.Date, exceptUserId: viewerId);
        var lockedByOthersSet = lockedByOthers.ToHashSet();

        var seats = bus.Seats.Where(s => s.IsActive).Select(s => new BusBooking.Application.DTOs.Booking.SeatDto
        {
            Id = s.Id,
            SeatNumber = s.SeatNumber,
            Row = s.Row,
            Column = s.Column,
            Deck = s.Deck,
            SeatType = s.SeatType.ToString(),
            IsAvailable = !bookedSeatIds.Contains(s.Id) && !lockedByOthersSet.Contains(s.Id),
            IsLocked = lockedByOthersSet.Contains(s.Id)
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

    /// <summary>
    /// Get predefined layout templates — operator picks one when adding a bus
    /// </summary>
    [HttpGet("layouts/templates")]
    [Authorize(Roles = "Operator")]
    public IActionResult GetLayoutTemplates()
    {
        var templates = new List<object>
        {
            new {
                name = "2+2 Seater (40 seats)",
                totalSeats = 40,
                rows = 10,
                columns = 4,
                hasUpperDeck = false,
                layoutJson = GenerateSeaterLayout(10, 4, "Seater")
            },
            new {
                name = "2+1 Seater (30 seats)",
                totalSeats = 30,
                rows = 10,
                columns = 3,
                hasUpperDeck = false,
                layoutJson = GenerateSeaterLayout(10, 3, "Seater")
            },
            new {
                name = "2+2 Semi-Sleeper (40 seats)",
                totalSeats = 40,
                rows = 10,
                columns = 4,
                hasUpperDeck = false,
                layoutJson = GenerateSeaterLayout(10, 4, "SemiSleeper")
            },
            new {
                name = "Sleeper (36 berths)",
                totalSeats = 36,
                rows = 9,
                columns = 4,
                hasUpperDeck = true,
                layoutJson = GenerateSleeperLayout(9)
            },
            new {
                name = "2+2 Double Decker (80 seats)",
                totalSeats = 80,
                rows = 10,
                columns = 4,
                hasUpperDeck = true,
                layoutJson = GenerateDoubleDeckLayout(10, 4)
            }
        };

        return Ok(templates);
    }

    private static string GenerateSeaterLayout(int rows, int columns, string type)
    {
        var seats = new List<object>();
        int seatNum = 1;
        var colLabels = new[] { "A", "B", "C", "D" };

        for (int row = 1; row <= rows; row++)
        {
            for (int col = 1; col <= columns; col++)
            {
                seats.Add(new
                {
                    seatNumber = $"{colLabels[col - 1]}{row}",
                    row,
                    column = col,
                    deck = "lower",
                    type
                });
                seatNum++;
            }
        }

        return System.Text.Json.JsonSerializer.Serialize(seats);
    }

    private static string GenerateSleeperLayout(int rows)
    {
        var seats = new List<object>();

        for (int row = 1; row <= rows; row++)
        {
            // Lower deck: L1, L2 per row
            seats.Add(new { seatNumber = $"L{row}A", row, column = 1, deck = "lower", type = "Sleeper" });
            seats.Add(new { seatNumber = $"L{row}B", row, column = 2, deck = "lower", type = "Sleeper" });
            // Upper deck: U1, U2 per row
            seats.Add(new { seatNumber = $"U{row}A", row, column = 1, deck = "upper", type = "Sleeper" });
            seats.Add(new { seatNumber = $"U{row}B", row, column = 2, deck = "upper", type = "Sleeper" });
        }

        return System.Text.Json.JsonSerializer.Serialize(seats);
    }

    private static string GenerateDoubleDeckLayout(int rows, int columns)
    {
        var seats = new List<object>();
        var colLabels = new[] { "A", "B", "C", "D" };

        for (int row = 1; row <= rows; row++)
        {
            for (int col = 1; col <= columns; col++)
            {
                // Lower deck
                seats.Add(new
                {
                    seatNumber = $"L{colLabels[col - 1]}{row}",
                    row,
                    column = col,
                    deck = "lower",
                    type = "Seater"
                });
                // Upper deck
                seats.Add(new
                {
                    seatNumber = $"U{colLabels[col - 1]}{row}",
                    row,
                    column = col,
                    deck = "upper",
                    type = "Seater"
                });
            }
        }

        return System.Text.Json.JsonSerializer.Serialize(seats);
    }

    /// <summary>
    /// Request to assign a bus to a route — operator submits, admin approves
    /// </summary>
    [HttpPost("{id}/assign-route")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> RequestRouteAssignment(
        Guid id, [FromBody] AssignRouteRequestDto request)
    {
        var operatorId = GetOperatorId();
        if (operatorId == null) return Unauthorized();

        var bus = await _unitOfWork.Buses.GetByIdAsync(id);
        if (bus == null || bus.OperatorId != operatorId.Value)
            return NotFound(new { message = "Bus not found." });

        if (bus.Status != BusStatus.Active)
            return BadRequest(new { message = "Bus must be active before assigning a route." });

        var route = await _unitOfWork.Routes.GetByIdAsync(request.RouteId);
        if (route == null || !route.IsActive)
            return BadRequest(new { message = "Invalid or inactive route." });

        var assignment = new BusRouteAssignment
        {
            BusId = id,
            RouteId = request.RouteId,
            OperatorId = operatorId.Value,
            DepartureTime = request.DepartureTime,
            ArrivalTime = request.ArrivalTime,
            DurationMinutes = request.DurationMinutes,
            BaseFare = request.BaseFare
        };

        await _unitOfWork.BusRouteAssignments.AddAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBusById), new { id }, new
        {
            message = "Route assignment request submitted. Awaiting admin approval.",
            assignmentId = assignment.Id
        });
    }

    /// <summary>
    /// Get pending route assignments — admin only
    /// </summary>
    [HttpGet("route-assignments/pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPendingAssignments()
    {
        var assignments = await _unitOfWork.BusRouteAssignments.GetPendingAsync();
        return Ok(assignments.Select(a => new
        {
            a.Id,
            a.BusId,
            BusNumber = a.Bus.BusNumber,
            BusName = a.Bus.BusName,
            OperatorName = a.Bus.Operator?.CompanyName,
            RouteId = a.RouteId,
            Route = $"{a.Route.SourceCity} → {a.Route.DestinationCity}",
            a.DepartureTime,
            a.ArrivalTime,
            a.DurationMinutes,
            a.BaseFare,
            a.CreatedAt
        }));
    }

    /// <summary>
    /// Approve or reject a route assignment — admin only
    /// </summary>
    [HttpPost("route-assignments/{assignmentId}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveRouteAssignment(
        Guid assignmentId, [FromBody] BusBooking.Application.DTOs.Admin.ApproveBusRequestDto request)
    {
        var assignment = await _unitOfWork.BusRouteAssignments.GetByIdAsync(assignmentId);
        if (assignment == null)
            return NotFound(new { message = "Assignment not found." });

        if (assignment.IsApproved || assignment.IsRejected)
            return BadRequest(new { message = "Assignment already reviewed." });

        if (request.IsApproved)
        {
            assignment.IsApproved = true;
            assignment.ReviewedAt = DateTime.UtcNow;
            assignment.AdminNotes = request.AdminNotes;

            // Apply to bus
            var bus = await _unitOfWork.Buses.GetByIdAsync(assignment.BusId);
            if (bus != null)
            {
                bus.RouteId = assignment.RouteId;
                bus.DepartureTime = assignment.DepartureTime;
                bus.ArrivalTime = assignment.ArrivalTime;
                bus.DurationMinutes = assignment.DurationMinutes;
                bus.BaseFare = assignment.BaseFare;
                _unitOfWork.Buses.Update(bus);
            }
        }
        else
        {
            assignment.IsRejected = true;
            assignment.ReviewedAt = DateTime.UtcNow;
            assignment.AdminNotes = request.AdminNotes;
        }

        _unitOfWork.BusRouteAssignments.Update(assignment);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new
        {
            message = request.IsApproved ? "Route assignment approved." : "Route assignment rejected.",
            assignmentId,
            status = request.IsApproved ? "Approved" : "Rejected"
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