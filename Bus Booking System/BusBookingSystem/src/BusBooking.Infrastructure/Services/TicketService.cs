using BusBooking.Application.Interfaces;
namespace BusBooking.Infrastructure.Services;
public class TicketService : ITicketService
{
    public Task<byte[]> GenerateTicketAsync(Guid bookingId) => Task.FromResult(Array.Empty<byte>());
    public Task<string> GenerateTicketFilePathAsync(Guid bookingId) => Task.FromResult(string.Empty);
}
