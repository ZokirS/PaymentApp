using Payment.Domain.Enums;

namespace Payment.Domain.Entities;

public class PaymentTransaction
{
    public string TransactionId { get; set; }
    public string SenderAccountId { get; set; }
    public string ReceiverAccountId { get; set; }
    public decimal Amount { get; set; }
    public int Currency { get; set; }
    public TransactionStatusEnum Status { get; set; }
    public DateTime CreatedAt { get; set; }
}