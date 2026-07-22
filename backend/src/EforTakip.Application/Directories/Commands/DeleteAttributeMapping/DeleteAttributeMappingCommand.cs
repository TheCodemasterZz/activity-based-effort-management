using MediatR;

namespace EforTakip.Application.Directories.Commands.DeleteAttributeMapping;

public sealed record DeleteAttributeMappingCommand(Guid Id) : IRequest;
