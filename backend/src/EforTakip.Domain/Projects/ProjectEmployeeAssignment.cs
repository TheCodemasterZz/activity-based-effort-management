using EforTakip.Domain.Common;

namespace EforTakip.Domain.Projects;

public sealed class ProjectEmployeeAssignment : Entity
{
    public Guid ProjectId { get; private set; }
    public Guid EmployeeId { get; private set; }

    private ProjectEmployeeAssignment()
    {
        // EF Core
    }

    internal static ProjectEmployeeAssignment Create(Guid projectId, Guid employeeId)
        => new()
        {
            ProjectId = projectId,
            EmployeeId = employeeId
        };
}
