using Payment.Domain.Entities;

namespace Payment.Infrastructure.Repositories;

public interface IUnitOfWork
{
    IRepository<Account> Accounts { get; }
    IRepository<PaymentTransaction> Transactions { get; }
    Task<int> SaveChangesAsync();
}