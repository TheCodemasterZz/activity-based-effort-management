using EforTakip.Application.Directories.Dtos;
using MediatR;

namespace EforTakip.Application.Directories.Queries.GetDirectoryUserById;

public sealed record GetDirectoryUserByIdQuery(Guid DirectoryUserId) : IRequest<DirectoryUserDetailDto>;
