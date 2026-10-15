using System.ComponentModel.DataAnnotations;

namespace Payment.Application.DTOs;

public class PaymentRequest
{
    [Required]
    public string TransactionId { get; set; }
    
    [Required]
    public string SenderAccount { get; set; }
    
    [Required]
    public string ReceiverAccount { get; set; }
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }
    
    [Required]
    public int Currency { get; set; }
}