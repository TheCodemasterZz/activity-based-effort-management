using EforTakip.Application.ValueStreams;
using EforTakip.Domain.ValueStreams;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Persistence.Repositories;

public sealed class ValueStreamRepository(EforTakipDbContext context)
    : RepositoryBase<ValueStream>(context), IValueStreamRepository
{
    public override async Task<ValueStream?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await Context.ValueStreams
            .Include(v => v.Stages)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
}
