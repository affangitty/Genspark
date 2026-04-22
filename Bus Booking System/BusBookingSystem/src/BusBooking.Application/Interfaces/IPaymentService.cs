namespace BusBooking.Application.Interfaces;

/// <summary>
/// Payment Processing Service (Dummy/Stripe/RazorPay)
/// </summary>
public interface IPaymentService
{
    Task<(bool success, string transactionId)> ProcessPaymentAsync(Guid bookingId, decimal amount);
    Task<bool> RefundPaymentAsync(Guid paymentId, decimal amount);
    Task<string> GetPaymentStatusAsync(string transactionId);
}
