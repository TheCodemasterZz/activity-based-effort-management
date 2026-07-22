using EforTakip.Application.Auth.Dtos;
using MediatR;

namespace EforTakip.Application.Auth.Commands.Login;

public sealed record LoginCommand(string Username, string Password) : IRequest<LoginResultDto>;
