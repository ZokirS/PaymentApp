using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id);
    Task AddAsync(T entity);
    void Update(T entity);
}
