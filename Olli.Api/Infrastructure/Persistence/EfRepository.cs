using Microsoft.EntityFrameworkCore;
using Olli.Api.Application.Repositories;

namespace Olli.Api.Infrastructure.Persistence;

public sealed class EfRepository<TEntity>(OlliDb db) : IRepository<TEntity>
    where TEntity : class
{
    private readonly DbSet<TEntity> _set = db.Set<TEntity>();

    public IQueryable<TEntity> Query() => _set;

    public ValueTask<TEntity?> FindAsync(object[] keyValues, CancellationToken cancellationToken) =>
        _set.FindAsync(keyValues, cancellationToken);

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken) =>
        _set.AddAsync(entity, cancellationToken).AsTask();

    public void Remove(TEntity entity) => _set.Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
