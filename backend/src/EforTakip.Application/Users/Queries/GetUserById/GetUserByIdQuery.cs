using EforTakip.Application.Users.Dtos;
using MediatR;

namespace EforTakip.Application.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDetailDto>;
