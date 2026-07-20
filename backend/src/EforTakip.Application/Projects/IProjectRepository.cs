using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Projects;

namespace EforTakip.Application.Projects;

public interface IProjectRepository : IRepository<Project>
{
    Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken);
}
