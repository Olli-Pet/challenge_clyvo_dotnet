namespace Olli.Api.Application.Repositories;

/// <summary>
/// Abstração mínima de acesso a agregados persistidos.
/// A implementação concreta fica na infraestrutura, mantendo os serviços testáveis.
/// </summary>
public interface IRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> Query();
    ValueTask<TEntity?> FindAsync(object[] keyValues, CancellationToken cancellationToken);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken);
    void Remove(TEntity entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
