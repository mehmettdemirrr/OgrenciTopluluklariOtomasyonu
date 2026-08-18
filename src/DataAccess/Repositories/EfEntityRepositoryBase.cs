using System.Linq.Expressions;
using Core.DataAccess;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

/// <summary>docs/MIMARI.md · Y-08: EF Core tipleri yalnızca burada; DAL daima materyalize sonuç döner.</summary>
public sealed class EfEntityRepositoryBase<TEntity>(AppDbContext context) : IEntityRepository<TEntity>
    where TEntity : class, IEntity
{
    public Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default) =>
        context.Set<TEntity>().FirstOrDefaultAsync(filter, cancellationToken);

    public Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = context.Set<TEntity>();
        if (filter is not null)
        {
            query = query.Where(filter);
        }

        return query.ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<TEntity>> GetListPagedAsync(
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = context.Set<TEntity>();
        if (filter is not null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PagedResult<TEntity>(items, totalCount, pageIndex, pageSize);
    }

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        context.Set<TEntity>().AddAsync(entity, cancellationToken).AsTask();

    public void Update(TEntity entity) => context.Set<TEntity>().Update(entity);

    public void Delete(TEntity entity) => context.Set<TEntity>().Remove(entity);
}
