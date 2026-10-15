using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.Application.Services;

public class ExternalBankClient: IExternalBankClient
{
    public async Task<bool> ExecuteTransferAsync(PaymentRequest request)
    {
        // Simulating network latency of an external service
        await Task.Delay(500);

        // Simulating a timeout or error for specific IDs (for testing purposes)
        if (request.TransactionId.Contains("TIMEOUT"))
        {
            throw new TimeoutException("External bank service timed out.");
        }

        if (request.TransactionId.Contains("REJECT"))
        {
            return false; // Bank rejected transaction
        }

        return true; // Success
    }
}