using EforTakip.Domain.Authorization;
using MediatR;

namespace EforTakip.Application.Roles.Queries.GetPermissionCatalog;

public sealed record GetPermissionCatalogQuery : IRequest<IReadOnlyCollection<PermissionDescriptor>>;
