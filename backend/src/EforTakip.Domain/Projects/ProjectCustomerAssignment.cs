using EforTakip.Domain.Common;

namespace EforTakip.Domain.Projects;

public sealed class ProjectCustomerAssignment : Entity
{
    public Guid ProjectId { get; private set; }
    public Guid CustomerId { get; private set; }

    private ProjectCustomerAssignment()
    {
        // EF Core
    }

    internal static ProjectCustomerAssignment Create(Guid projectId, Guid customerId)
        => new()
        {
            ProjectId = projectId,
            CustomerId = customerId
        };
}
