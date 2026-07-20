using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Common;

namespace EforTakip.Persistence.Repositories;

public class RepositoryBase<T>(EforTakipDbContext context) : IRepository<T>
    where T : class, IAggregateRoot
{
    protected EforTakipDbContext Context { get; } = context;

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await Context.Set<T>().FindAsync([id], cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken)
        => await Context.Set<T>().AddAsync(entity, cancellationToken);

    public void Update(T entity) => Context.Set<T>().Update(entity);

    public void Remove(T entity) => Context.Set<T>().Remove(entity);
}
