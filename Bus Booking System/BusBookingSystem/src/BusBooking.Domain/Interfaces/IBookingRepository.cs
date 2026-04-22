using BusBooking.Domain.Entities;

namespace BusBooking.Domain.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<Booking?> GetByReferenceAsync(string bookingReference);
    Task<IEnumerable<Booking>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Booking>> GetByBusIdAsync(Guid busId);
    Task<IEnumerable<Booking>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Booking>> GetFutureBookingsByBusIdAsync(Guid busId);
    Task<IEnumerable<Guid>> GetBookedSeatIdsByBusAndDateAsync(Guid busId, DateTime journeyDate);
    Task AddAsync(Booking booking);
    void Update(Booking booking);
    void Delete(Booking booking);
}