using EforTakip.Application.Directories.Dtos;
using MediatR;

namespace EforTakip.Application.Directories.Queries.GetAttributeMappings;

public sealed record GetAttributeMappingsQuery : IRequest<IReadOnlyCollection<DirectoryAttributeMappingDto>>;
