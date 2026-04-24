using BusBooking.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace BusBooking.Infrastructure.Services;

/// <summary>
/// SMTP email service for sending booking confirmations and notifications
/// Configuration via appsettings.json SMTP section
/// </summary>
public class MailService : IMailService
{
    private readonly IConfiguration _configuration;
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _senderEmail;
    private readonly string _senderPassword;

    public MailService(IConfiguration configuration)
    {
        _configuration = configuration;
        _smtpServer = configuration["SMTP:Server"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(configuration["SMTP:Port"] ?? "587");
        _senderEmail = configuration["SMTP:Email"] ?? "your-email@gmail.com";
        _senderPassword = configuration["SMTP:Password"] ?? "your-app-password";
    }

    /// <summary>
    /// Send a generic email
    /// </summary>
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            using (var client = new SmtpClient(_smtpServer, _smtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(_senderEmail, _senderPassword);
                client.Timeout = 5000;

                var message = new MailMessage
                {
                    From = new MailAddress(_senderEmail, "Bus Booking System"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(to);
                await SendWithTimeout(client, message, timeoutMs: 5000);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw - email service should be non-blocking
            System.Diagnostics.Debug.WriteLine($"Email send failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Send booking confirmation email with ticket attachment
    /// </summary>
    public async Task SendBookingConfirmationAsync(string to, string bookingId, string ticketPath)
    {
        var subject = $"Booking Confirmation - Ref: {bookingId}";
        
        var body = new StringBuilder();
        body.AppendLine("<html>");
        body.AppendLine("<body style=\"font-family: Arial, sans-serif;\">");
        body.AppendLine("<h2>Booking Confirmation</h2>");
        body.AppendLine($"<p>Dear Customer,</p>");
        body.AppendLine($"<p>Your bus booking has been confirmed successfully!</p>");
        body.AppendLine($"<p><strong>Booking Reference:</strong> {bookingId}</p>");
        body.AppendLine($"<p>Your ticket has been attached to this email. Please save it for your records.</p>");
        body.AppendLine($"<p><strong>Please arrive at the boarding point 15 minutes before departure.</strong></p>");
        body.AppendLine($"<p>Thank you for choosing our bus booking service!</p>");
        body.AppendLine($"<p>Best regards,<br/>Bus Booking Team</p>");
        body.AppendLine("</body>");
        body.AppendLine("</html>");

        try
        {
            using (var client = new SmtpClient(_smtpServer, _smtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(_senderEmail, _senderPassword);
                client.Timeout = 5000;

                var message = new MailMessage
                {
                    From = new MailAddress(_senderEmail, "Bus Booking System"),
                    Subject = subject,
                    Body = body.ToString(),
                    IsBodyHtml = true
                };

                message.To.Add(to);

                // Attach ticket if file exists
                if (!string.IsNullOrEmpty(ticketPath) && File.Exists(ticketPath))
                {
                    var attachment = new Attachment(ticketPath);
                    message.Attachments.Add(attachment);
                }

                await SendWithTimeout(client, message, timeoutMs: 7000);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Booking confirmation email failed: {ex.Message}");
        }
    }

    public async Task SendBookingConfirmationDetailedAsync(
        string to,
        string bookingReference,
        string busNumber,
        string route,
        DateTime journeyDate,
        string boardingAddress,
        string dropOffAddress,
        decimal totalAmount,
        string ticketPath)
    {
        var subject = $"Booking Confirmed — {busNumber} — Ref: {bookingReference}";
        var safeRoute = WebUtility.HtmlEncode(route);
        var safeBusNumber = WebUtility.HtmlEncode(busNumber);
        var safeRef = WebUtility.HtmlEncode(bookingReference);
        var safeBoard = WebUtility.HtmlEncode(boardingAddress);
        var safeDrop = WebUtility.HtmlEncode(dropOffAddress);

        var body = $@"
<html>
  <body style=""font-family: Arial, sans-serif;"">
    <h2>Booking Confirmed</h2>
    <p><strong>Booking Reference:</strong> {safeRef}</p>
    <p><strong>Bus:</strong> {safeBusNumber}</p>
    <p><strong>Route:</strong> {safeRoute}</p>
    <p><strong>Journey Date:</strong> {journeyDate:dd MMM yyyy}</p>
    <p><strong>Boarding:</strong> {safeBoard}<br/>
       <strong>Drop-off:</strong> {safeDrop}</p>
    <p><strong>Total Paid:</strong> ₹{totalAmount:F2}</p>
    <p>Your ticket PDF is attached (if configured) and is also downloadable from your dashboard.</p>
    <p style=""margin-top:16px""><strong>Please arrive 15 minutes before departure.</strong></p>
    <p>— Bus Booking System</p>
  </body>
</html>";

        await SendEmailWithOptionalAttachment(to, subject, body, ticketPath, timeoutMs: 7000);
    }

    /// <summary>
    /// Send cancellation notice with refund details
    /// </summary>
    public async Task SendCancellationNoticeAsync(string to, string bookingId, decimal refundAmount)
    {
        var subject = $"Booking Cancelled - Ref: {bookingId}";
        
        var body = new StringBuilder();
        body.AppendLine("<html>");
        body.AppendLine("<body style=\"font-family: Arial, sans-serif;\">");
        body.AppendLine("<h2>Booking Cancellation</h2>");
        body.AppendLine($"<p>Dear Customer,</p>");
        body.AppendLine($"<p>Your booking has been cancelled.</p>");
        body.AppendLine($"<p><strong>Booking Reference:</strong> {bookingId}</p>");
        body.AppendLine($"<p><strong>Refund Amount:</strong> ₹{refundAmount:F2}</p>");
        body.AppendLine($"<p>The refund will be processed within 5-7 business days to your original payment method.</p>");
        body.AppendLine($"<p>If you have any questions, please contact our support team.</p>");
        body.AppendLine($"<p>Best regards,<br/>Bus Booking Team</p>");
        body.AppendLine("</body>");
        body.AppendLine("</html>");

        await SendEmailAsync(to, subject, body.ToString());
    }

    /// <summary>
    /// Notify operator that their account has been disabled
    /// </summary>
    public async Task SendOperatorDisabledNoticeAsync(string to, string operatorName, string reason, string? alternativeOperators = null)
    {
        var subject = "Account Status Update - Bus Booking Platform";
        
        var body = new StringBuilder();
        body.AppendLine("<html>");
        body.AppendLine("<body style=\"font-family: Arial, sans-serif;\">");
        body.AppendLine("<h2>Account Disabled</h2>");
        body.AppendLine($"<p>Dear {operatorName},</p>");
        body.AppendLine($"<p>Your operator account on the Bus Booking Platform has been disabled by the administrator.</p>");
        body.AppendLine($"<p><strong>Reason:</strong> {WebUtility.HtmlEncode(reason)}</p>");
        body.AppendLine($"<p>If you believe this is an error, please contact our support team immediately.</p>");
        
        if (!string.IsNullOrEmpty(alternativeOperators))
        {
            body.AppendLine($"<p><strong>Note:</strong> {alternativeOperators}</p>");
        }
        
        body.AppendLine($"<p>Best regards,<br/>Bus Booking Administrator</p>");
        body.AppendLine("</body>");
        body.AppendLine("</html>");

        await SendEmailAsync(to, subject, body.ToString());
    }

    private static async Task SendWithTimeout(SmtpClient client, MailMessage message, int timeoutMs)
    {
        var sendTask = client.SendMailAsync(message);
        var done = await Task.WhenAny(sendTask, Task.Delay(timeoutMs)).ConfigureAwait(false);
        if (done != sendTask)
            throw new TimeoutException($"SMTP send timed out after {timeoutMs}ms.");
        await sendTask.ConfigureAwait(false);
    }

    private async Task SendEmailWithOptionalAttachment(
        string to,
        string subject,
        string body,
        string? attachmentPath,
        int timeoutMs)
    {
        try
        {
            using (var client = new SmtpClient(_smtpServer, _smtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(_senderEmail, _senderPassword);
                client.Timeout = 5000;

                var message = new MailMessage
                {
                    From = new MailAddress(_senderEmail, "Bus Booking System"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(to);
                if (!string.IsNullOrWhiteSpace(attachmentPath) && File.Exists(attachmentPath))
                {
                    message.Attachments.Add(new Attachment(attachmentPath));
                }

                await SendWithTimeout(client, message, timeoutMs);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Email send failed: {ex.Message}");
        }
    }
}

