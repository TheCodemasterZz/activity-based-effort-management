using EforTakip.Domain.Common;

namespace EforTakip.Application.Common.Interfaces;

public interface IRepository<T> where T : class, IAggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(T entity, CancellationToken cancellationToken);

    void Update(T entity);

    void Remove(T entity);
}
