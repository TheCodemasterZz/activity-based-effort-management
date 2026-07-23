using EforTakip.Domain.Authorization;
using MediatR;

namespace EforTakip.Application.Roles.Queries.GetPermissionCatalog;

public sealed class GetPermissionCatalogQueryHandler
    : IRequestHandler<GetPermissionCatalogQuery, IReadOnlyCollection<string>>
{
    public Task<IReadOnlyCollection<string>> Handle(
        GetPermissionCatalogQuery request, CancellationToken cancellationToken)
        => Task.FromResult(Permissions.All);
}
