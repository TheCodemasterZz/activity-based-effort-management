using MediatR;

namespace EforTakip.Application.Users.Commands.ResetInternalUserPassword;

public sealed record ResetInternalUserPasswordCommand(Guid UserId, string NewPassword) : IRequest;
