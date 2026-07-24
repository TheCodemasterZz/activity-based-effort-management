using EforTakip.Domain.Common;

namespace EforTakip.Domain.Projects;

public sealed class ProjectUserAssignment : Entity
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }

    private ProjectUserAssignment()
    {
        // EF Core
    }

    internal static ProjectUserAssignment Create(Guid projectId, Guid userId)
        => new()
        {
            ProjectId = projectId,
            UserId = userId
        };
}
