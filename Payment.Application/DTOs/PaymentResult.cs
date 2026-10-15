namespace Payment.Application.DTOs;

public class PaymentResult
{
    public string TransactionId { get; set; }
    public int Resp { get; set; } // 0 - OK, 1 - Pending/Timeout, -1 - Error
    public string Status { get; set; }
}