using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using Payment.Infrastructure.Configuration;

namespace Payment.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<PaymentTransaction> Transactions => Set<PaymentTransaction>();
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        => Database.EnsureCreated();
        
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentTransaction>()
            .HasKey(t => t.TransactionId);

        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
    }
}