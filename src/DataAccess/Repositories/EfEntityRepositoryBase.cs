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

    public Task<PagedResult<TEntity>> GetListPagedAsync(
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default) =>
        // Y-64: sıra verilmediğinde Id artan — sırasız Skip/Take bu sınıftan çıkamaz.
        PageAsync(pageIndex, pageSize, filter, q => q.OrderBy(e => e.Id), cancellationToken);

    public Task<PagedResult<TEntity>> GetListPagedAsync<TKey>(
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter,
        Expression<Func<TEntity, TKey>> orderBy,
        bool descending = false,
        CancellationToken cancellationToken = default) =>
        PageAsync(
            pageIndex,
            pageSize,
            filter,
            // Y-64: eşit anahtarlı satırlar sayfalar arasında yer değiştirmesin diye Id son kırıcı.
            q => descending
                ? q.OrderByDescending(orderBy).ThenBy(e => e.Id)
                : q.OrderBy(orderBy).ThenBy(e => e.Id),
            cancellationToken);

    private async Task<PagedResult<TEntity>> PageAsync(
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> order,
        CancellationToken cancellationToken)
    {
        IQueryable<TEntity> query = context.Set<TEntity>();
        if (filter is not null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await order(query).Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PagedResult<TEntity>(items, totalCount, pageIndex, pageSize);
    }

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        context.Set<TEntity>().AddAsync(entity, cancellationToken).AsTask();

    public void Update(TEntity entity) => context.Set<TEntity>().Update(entity);

    public void Delete(TEntity entity) => context.Set<TEntity>().Remove(entity);
}
