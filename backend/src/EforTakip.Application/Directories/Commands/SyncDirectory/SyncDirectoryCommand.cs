using EforTakip.Application.Directories.Dtos;
using MediatR;

namespace EforTakip.Application.Directories.Commands.SyncDirectory;

public sealed record SyncDirectoryCommand(Guid DirectoryId) : IRequest<DirectorySyncResultDto>;
