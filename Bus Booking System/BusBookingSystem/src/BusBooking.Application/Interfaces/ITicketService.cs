namespace BusBooking.Application.Interfaces;

/// <summary>
/// Ticket/PDF Generation Service
/// </summary>
public interface ITicketService
{
    Task<byte[]> GenerateTicketAsync(Guid bookingId);
    Task<string> GenerateTicketFilePathAsync(Guid bookingId);
}
