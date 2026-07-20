using MediatR;

namespace EforTakip.Application.ValueStreams.Commands.AssignActivityToStage;

public sealed record AssignActivityToStageCommand(Guid ValueStreamStageId, Guid ActivityId) : IRequest;
