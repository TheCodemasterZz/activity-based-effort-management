using EforTakip.Domain.Authorization;
using MediatR;

namespace EforTakip.Application.Roles.Queries.GetPermissionCatalog;

public sealed class GetPermissionCatalogQueryHandler
    : IRequestHandler<GetPermissionCatalogQuery, IReadOnlyCollection<PermissionDescriptor>>
{
    public Task<IReadOnlyCollection<PermissionDescriptor>> Handle(
        GetPermissionCatalogQuery request, CancellationToken cancellationToken)
        => Task.FromResult(Permissions.AllDescriptors);
}
