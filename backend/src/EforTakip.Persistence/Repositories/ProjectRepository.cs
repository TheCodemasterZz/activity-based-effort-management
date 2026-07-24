using EforTakip.Application.Projects;
using EforTakip.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Persistence.Repositories;

public sealed class ProjectRepository(EforTakipDbContext context)
    : RepositoryBase<Project>(context), IProjectRepository
{
    public override async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await Context.Projects
            .Include(p => p.CustomerAssignments)
            .Include(p => p.UserAssignments)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken)
        => await Context.Projects.AnyAsync(p => p.Name == name, cancellationToken);
}
