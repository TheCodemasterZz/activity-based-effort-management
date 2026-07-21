using EforTakip.Application.Directories.Dtos;
using MediatR;

namespace EforTakip.Application.Directories.Queries.GetDirectoryById;

public sealed record GetDirectoryByIdQuery(Guid DirectoryId) : IRequest<DirectoryDto>;
