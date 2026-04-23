using BusBooking.Application.Interfaces;
using BusBooking.Domain.Interfaces;
using BusBooking.Domain.Enums;

namespace BusBooking.Infrastructure.Services;

/// <summary>
/// Dummy payment gateway service for development
/// In production, replace with Stripe, RazorPay, or similar
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly Random _random = new Random();

    public PaymentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Simulate payment processing
    /// In production: calls actual payment gateway API
    /// </summary>
    public async Task<(bool success, string transactionId)> ProcessPaymentAsync(Guid bookingId, decimal amount)
    {
        try
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
                return (false, string.Empty);

            // Simulate payment with 90% success rate for demo
            var isSuccess = _random.Next(100) < 90;
            var transactionId = $"TXN-{Guid.NewGuid():N}";

            // Create payment record
            var payment = booking.Payment ?? new()
            {
                BookingId = bookingId,
                Amount = amount,
                TransactionId = transactionId,
                Status = isSuccess ? PaymentStatus.Success : PaymentStatus.Failed,
                PaymentMethod = "DummyGateway",
                PaidAt = isSuccess ? DateTime.UtcNow : null,
                FailureReason = isSuccess ? null : "Simulated payment decline"
            };

            if (booking.Payment == null)
                await _unitOfWork.Bookings.AddPaymentAsync(payment);
            else
            {
                payment.TransactionId = transactionId;
                payment.Status = isSuccess ? PaymentStatus.Success : PaymentStatus.Failed;
                if (isSuccess)
                    payment.PaidAt = DateTime.UtcNow;
                else
                    payment.FailureReason = "Simulated payment decline";
            }

            await _unitOfWork.SaveChangesAsync();
            return (isSuccess, transactionId);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// Process refunds for cancelled bookings
    /// </summary>
    public async Task<bool> RefundPaymentAsync(Guid paymentId, decimal amount)
    {
        try
        {
            var bookings = await _unitOfWork.Bookings.GetByDateRangeAsync(DateTime.UtcNow.AddYears(-2), DateTime.UtcNow.AddYears(2));
            var payment = bookings
                .Select(b => b.Payment)
                .FirstOrDefault(p => p != null && p.Id == paymentId);

            if (payment == null)
                return false;

            payment.RefundAmount = amount;
            payment.RefundedAt = DateTime.UtcNow;
            payment.Status = amount < payment.Amount ? PaymentStatus.PartialRefund : PaymentStatus.Refunded;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get payment status
    /// </summary>
    public async Task<string> GetPaymentStatusAsync(string transactionId)
    {
        await Task.CompletedTask;
        // In production: query payment gateway API
        return "Success";
    }
}

