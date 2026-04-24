using BusBooking.Application.Interfaces;
using BusBooking.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace BusBooking.API.Hubs;

/// <summary>
/// SignalR Hub for real-time seat locking and availability updates
/// </summary>
[Authorize]
public class SeatHub : Hub
{
    private readonly ISeatLockService _seatLockService;
    private readonly IUnitOfWork _unitOfWork;
    private static readonly ConcurrentDictionary<string, string> _userConnections = new();

    public SeatHub(ISeatLockService seatLockService, IUnitOfWork unitOfWork)
    {
        _seatLockService = seatLockService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId()?.ToString() ?? Context.ConnectionId;
        _userConnections.TryAdd(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _userConnections.TryRemove(Context.ConnectionId, out _);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Lock a seat for the current user
    /// Called when user clicks on a seat in the bus layout
    /// </summary>
    public async Task<bool> LockSeat(Guid busId, Guid seatId, DateTime journeyDate)
    {
        var userGuid = GetUserId();
        if (userGuid == null)
            return false;

        var cfg = await _unitOfWork.PlatformConfig.GetCurrentAsync();
        var lockDurationSeconds = Math.Clamp((cfg?.SeatLockDurationMinutes ?? 10) * 60, 60, 7200);
        var success = await _seatLockService.LockSeatAsync(seatId, userGuid.Value, lockDurationSeconds, journeyDate.Date);

        if (success)
        {
            // Broadcast seat lock to all clients viewing this bus
            await Clients.Group($"Bus_{busId}_{journeyDate:yyyyMMdd}")
                .SendAsync("SeatLocked", seatId, userGuid.Value);
        }

        return success;
    }

    /// <summary>
    /// Unlock a seat (user deselects or cancels booking)
    /// </summary>
    public async Task<bool> UnlockSeat(Guid busId, Guid seatId, DateTime journeyDate)
    {
        var userGuid = GetUserId();
        if (userGuid == null)
            return false;

        var success = await _seatLockService.UnlockSeatAsync(seatId, userGuid.Value, journeyDate.Date);

        if (success)
        {
            // Broadcast seat unlock to all clients viewing this bus
            await Clients.Group($"Bus_{busId}_{journeyDate:yyyyMMdd}")
                .SendAsync("SeatUnlocked", seatId);
        }

        return success;
    }

    /// <summary>
    /// Extend seat lock duration (user still in checkout)
    /// </summary>
    public async Task<bool> ExtendLock(Guid seatId, DateTime journeyDate, int additionalSeconds = 300)
    {
        return await _seatLockService.ExtendLockAsync(seatId, additionalSeconds, journeyDate.Date);
    }

    /// <summary>
    /// Join a bus viewing group to get real-time updates
    /// </summary>
    public async Task JoinBusGroup(Guid busId, DateTime journeyDate)
    {
        var groupName = $"Bus_{busId}_{journeyDate:yyyyMMdd}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Leave a bus viewing group
    /// </summary>
    public async Task LeaveBusGroup(Guid busId, DateTime journeyDate)
    {
        var groupName = $"Bus_{busId}_{journeyDate:yyyyMMdd}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Broadcast available seats for a bus
    /// </summary>
    public async Task BroadcastAvailableSeats(Guid busId, DateTime journeyDate, List<Guid> availableSeatIds)
    {
        var groupName = $"Bus_{busId}_{journeyDate:yyyyMMdd}";
        await Clients.Group(groupName).SendAsync("AvailableSeatsUpdated", availableSeatIds);
    }

    /// <summary>
    /// Request current seat status
    /// </summary>
    public async Task RequestSeatStatus(Guid busId, DateTime journeyDate)
    {
        var groupName = $"Bus_{busId}_{journeyDate:yyyyMMdd}";
        // Server should respond with current seat status
        await Clients.Caller.SendAsync("RequestSeatStatusAck", busId, journeyDate);
    }

    private Guid? GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirst("sub");
        if (claim == null) return null;
        return Guid.TryParse(claim.Value, out var id) ? id : null;
    }
}

