using BusBooking.Application.Interfaces;
namespace BusBooking.Infrastructure.Services;
public class MailService : IMailService
{
    public Task SendEmailAsync(string to, string subject, string body) => Task.CompletedTask;
    public Task SendBookingConfirmationAsync(string to, string bookingId, string ticketPath) => Task.CompletedTask;
    public Task SendCancellationNoticeAsync(string to, string bookingId, decimal refundAmount) => Task.CompletedTask;
    public Task SendOperatorDisabledNoticeAsync(string to, string operatorName, string? alternativeOperators = null) => Task.CompletedTask;
}
