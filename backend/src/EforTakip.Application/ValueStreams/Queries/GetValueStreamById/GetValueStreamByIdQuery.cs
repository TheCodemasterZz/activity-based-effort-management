using EforTakip.Application.ValueStreams.Dtos;
using MediatR;

namespace EforTakip.Application.ValueStreams.Queries.GetValueStreamById;

public sealed record GetValueStreamByIdQuery(Guid ValueStreamId) : IRequest<ValueStreamDetailDto>;
