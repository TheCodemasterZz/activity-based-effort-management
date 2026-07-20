using MediatR;

namespace EforTakip.Application.ValueStreams.Commands.AddStage;

public sealed record AddStageCommand(Guid ValueStreamId, string Name, int Order) : IRequest<Guid>;
