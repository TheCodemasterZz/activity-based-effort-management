using MediatR;

namespace EforTakip.Application.Directories.Commands.DeleteDirectory;

public sealed record DeleteDirectoryCommand(Guid Id) : IRequest;
