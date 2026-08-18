using System.Linq.Expressions;
using Core.Entities;

namespace Core.DataAccess;

/// <summary>
/// docs/MIMARI.md · Y-08: EF Core'a ait hiçbir tip (IQueryable, DbSet vb.) bu sözleşmeye sızmaz.
/// Y-15 / A-05: bu arayüz kaydı işaretler, kaydetmez — bkz. <see cref="IUnitOfWork"/>.
/// </summary>
public interface IEntityRepository<TEntity> where TEntity : class, IEntity
{
    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default);

    Task<PagedResult<TEntity>> GetListPagedAsync(
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Delete(TEntity entity);
}
