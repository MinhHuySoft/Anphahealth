using Microsoft.EntityFrameworkCore;

namespace AnphaHealth.Web.Data.Repository;

public interface IRepository<TEntity> where TEntity : class
{
    Task<TResult> ReadAsync<TResult>(Func<IQueryable<TEntity>, CancellationToken, Task<TResult>> query,
        CancellationToken cancellationToken = default);
    Task<TResult> WriteAsync<TResult>(Func<AnphaHealthDbContext, DbSet<TEntity>, CancellationToken, Task<TResult>> apply,
        CancellationToken cancellationToken = default);
}
