using MediatR;

namespace EforTakip.Application.Activities.Commands.CreateActivity;

public sealed record CreateActivityCommand(string Name, string? Description, Guid? ParentActivityId) : IRequest<Guid>;
