using EforTakip.Application.Directories.Dtos;
using MediatR;

namespace EforTakip.Application.Directories.Queries.GetAttributeMappings;

public sealed record GetAttributeMappingsQuery(Guid DirectoryId)
    : IRequest<IReadOnlyCollection<DirectoryAttributeMappingDto>>;
