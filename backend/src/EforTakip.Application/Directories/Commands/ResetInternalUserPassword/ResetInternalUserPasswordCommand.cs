using MediatR;

namespace EforTakip.Application.Directories.Commands.ResetInternalUserPassword;

public sealed record ResetInternalUserPasswordCommand(Guid DirectoryUserId, string NewPassword) : IRequest;
