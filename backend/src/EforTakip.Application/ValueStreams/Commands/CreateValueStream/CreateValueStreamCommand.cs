using MediatR;

namespace EforTakip.Application.ValueStreams.Commands.CreateValueStream;

public sealed record CreateValueStreamCommand(string Name, string? Description) : IRequest<Guid>;
