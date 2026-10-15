using Payment.Domain.Entities;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories;

public class UnitOfWork: IUnitOfWork
{
    private readonly AppDbContext _context;
    
    public IRepository<Account> Accounts { get; }
    public IRepository<PaymentTransaction> Transactions { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Accounts = new Repository<Account>(_context);
        Transactions = new Repository<PaymentTransaction>(_context);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}