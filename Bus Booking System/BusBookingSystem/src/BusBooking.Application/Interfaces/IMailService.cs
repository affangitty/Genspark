namespace BusBooking.Application.Interfaces;

/// <summary>
/// Email Service for sending notifications
/// </summary>
public interface IMailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendBookingConfirmationAsync(string to, string bookingId, string ticketPath);
    Task SendCancellationNoticeAsync(string to, string bookingId, decimal refundAmount);
    Task SendOperatorDisabledNoticeAsync(string to, string operatorName, string reason, string? alternativeOperators = null);
}
