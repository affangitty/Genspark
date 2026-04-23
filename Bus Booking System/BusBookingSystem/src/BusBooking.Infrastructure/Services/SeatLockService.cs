using BusBooking.Application.Interfaces;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Interfaces;

namespace BusBooking.Infrastructure.Services;

/// <summary>
/// Real-time seat locking service for preventing double bookings
/// Locks seats for a configurable duration (default 10 minutes)
/// </summary>
public class SeatLockService : ISeatLockService
{
    private readonly IUnitOfWork _unitOfWork;

    public SeatLockService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> LockSeatAsync(Guid seatId, Guid userId, int lockDurationSeconds = 600, DateTime? journeyDate = null)
    {
        try
        {
            var seat = await _unitOfWork.Seats.GetByIdAsync(seatId);
            if (seat == null)
                return false;

            var targetJourneyDate = (journeyDate ?? DateTime.UtcNow).Date;

            // Check if already locked and not expired
            var existingLock = await _unitOfWork.SeatLocks.GetActiveLockAsync(seatId, targetJourneyDate);
            if (existingLock != null && !existingLock.IsReleased && existingLock.ExpiresAt > DateTime.UtcNow)
                return false; // Already locked by someone else

            // Create new lock
            var seatLock = new SeatLock
            {
                SeatId = seatId,
                UserId = userId,
                BusId = seat.BusId,
                JourneyDate = targetJourneyDate,
                LockedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddSeconds(lockDurationSeconds),
                IsReleased = false
            };

            await _unitOfWork.SeatLocks.AddAsync(seatLock);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UnlockSeatAsync(Guid seatId, Guid userId, DateTime? journeyDate = null)
    {
        try
        {
            var targetJourneyDate = (journeyDate ?? DateTime.UtcNow).Date;
            var existingLock = await _unitOfWork.SeatLocks.GetActiveLockAsync(seatId, targetJourneyDate);
            
            if (existingLock == null || existingLock.UserId != userId)
                return false;

            existingLock.IsReleased = true;
            _unitOfWork.SeatLocks.Update(existingLock);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsSeatLockedAsync(Guid seatId, DateTime? journeyDate = null)
    {
        var targetJourneyDate = (journeyDate ?? DateTime.UtcNow).Date;
        var existingLock = await _unitOfWork.SeatLocks.GetActiveLockAsync(seatId, targetJourneyDate);
        return existingLock != null && !existingLock.IsReleased && existingLock.ExpiresAt > DateTime.UtcNow;
    }

    public async Task<bool> ExtendLockAsync(Guid seatId, int additionalSeconds = 300, DateTime? journeyDate = null)
    {
        try
        {
            var targetJourneyDate = (journeyDate ?? DateTime.UtcNow).Date;
            var existingLock = await _unitOfWork.SeatLocks.GetActiveLockAsync(seatId, targetJourneyDate);
            
            if (existingLock == null || existingLock.IsReleased)
                return false;

            existingLock.ExpiresAt = existingLock.ExpiresAt.AddSeconds(additionalSeconds);
            _unitOfWork.SeatLocks.Update(existingLock);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task CleanupExpiredLocksAsync()
    {
        try
        {
            await _unitOfWork.SeatLocks.DeleteExpiredLocksAsync();
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            // Log error but don't throw
        }
    }
}

