using Microsoft.EntityFrameworkCore;

namespace AnphaHealth.Web.Data.Repository;

internal sealed class EfRepository<TEntity>(IDbContextFactory<AnphaHealthDbContext> factory)
    : IRepository<TEntity> where TEntity : class
{
    public async Task<TResult> ReadAsync<TResult>(
        Func<IQueryable<TEntity>, CancellationToken, Task<TResult>> query, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        return await query(context.Set<TEntity>().AsNoTracking(), cancellationToken);
    }

    public async Task<TResult> WriteAsync<TResult>(
        Func<AnphaHealthDbContext, DbSet<TEntity>, CancellationToken, Task<TResult>> apply,
        CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var result = await apply(context, context.Set<TEntity>(), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return result;
    }
}
