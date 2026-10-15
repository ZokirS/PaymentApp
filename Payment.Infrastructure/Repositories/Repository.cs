using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories;

public class Repository<T>(AppDbContext context) : IRepository<T>
    where T : class
{
    public async Task<T?> GetByIdAsync(object id)
    {
        return await context.Set<T>().FindAsync(id);
    }

    public async Task AddAsync(T entity)
    {
        await context.Set<T>().AddAsync(entity);
    }

    public void Update(T entity)
    {
        context.Set<T>().Update(entity);
    }
}