using Payment.Application.DTOs;

namespace Payment.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
}