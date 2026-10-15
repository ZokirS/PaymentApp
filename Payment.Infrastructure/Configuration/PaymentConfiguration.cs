using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Configuration;

public class PaymentConfiguration: IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasData(
            new Account
            {
                Id = "ACC_DEBIT_001", 
                Balance = 1000000,
                Currency = 860
            },
            new Account
            {
                Id = "ACC_CREDIT_001", 
                Balance = 0,
                Currency = 860
            });
    }
}