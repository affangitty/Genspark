using BusBooking.Application.Interfaces;
namespace BusBooking.Infrastructure.Services;
public class PaymentService : IPaymentService
{
    public Task<(bool success, string transactionId)> ProcessPaymentAsync(Guid bookingId, decimal amount) => Task.FromResult((true, Guid.NewGuid().ToString()));
    public Task<bool> RefundPaymentAsync(Guid paymentId, decimal amount) => Task.FromResult(true);
    public Task<string> GetPaymentStatusAsync(string transactionId) => Task.FromResult("Success");
}
