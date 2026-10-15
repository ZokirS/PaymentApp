using Payment.Application.DTOs;

namespace Payment.Application.Interfaces;

public interface IExternalBankClient
{
    Task<bool> ExecuteTransferAsync(PaymentRequest request);
}