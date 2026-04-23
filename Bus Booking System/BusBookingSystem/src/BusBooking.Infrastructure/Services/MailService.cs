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

                var message = new MailMessage
                {
                    From = new MailAddress(_senderEmail, "Bus Booking System"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(to);
                await client.SendMailAsync(message);
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

                await client.SendMailAsync(message);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Booking confirmation email failed: {ex.Message}");
        }
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
    public async Task SendOperatorDisabledNoticeAsync(string to, string operatorName, string? alternativeOperators = null)
    {
        var subject = "Account Status Update - Bus Booking Platform";
        
        var body = new StringBuilder();
        body.AppendLine("<html>");
        body.AppendLine("<body style=\"font-family: Arial, sans-serif;\">");
        body.AppendLine("<h2>Account Disabled</h2>");
        body.AppendLine($"<p>Dear {operatorName},</p>");
        body.AppendLine($"<p>Your operator account on the Bus Booking Platform has been disabled by the administrator.</p>");
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
}

