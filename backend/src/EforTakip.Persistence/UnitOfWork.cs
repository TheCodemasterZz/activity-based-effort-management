using EforTakip.Application.Common.Interfaces;

namespace EforTakip.Persistence;

public sealed class UnitOfWork(EforTakipDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
