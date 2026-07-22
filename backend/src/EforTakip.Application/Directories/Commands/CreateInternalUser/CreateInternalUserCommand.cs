using MediatR;

namespace EforTakip.Application.Directories.Commands.CreateInternalUser;

public sealed record CreateInternalUserCommand(
    Guid DirectoryId,
    string Username,
    string Password,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? Email) : IRequest<Guid>;
