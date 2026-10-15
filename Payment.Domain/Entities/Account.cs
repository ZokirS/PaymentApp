using System.ComponentModel.DataAnnotations;

namespace Payment.Domain.Entities;

public class Account
{
    public string Id { get; set; }
    public decimal Balance { get; set; }
    public int Currency { get; set; }
    [ConcurrencyCheck]
    public int Version { get; set; } = 1;
}