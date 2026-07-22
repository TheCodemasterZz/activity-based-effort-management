using MediatR;

namespace EforTakip.Application.Projects.Commands.DeleteProjectTask;

public sealed record DeleteProjectTaskCommand(Guid Id) : IRequest;
