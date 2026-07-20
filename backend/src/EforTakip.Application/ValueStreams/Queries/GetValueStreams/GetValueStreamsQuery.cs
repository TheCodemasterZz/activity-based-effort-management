using EforTakip.Application.Common.Models;
using EforTakip.Application.ValueStreams.Dtos;
using MediatR;

namespace EforTakip.Application.ValueStreams.Queries.GetValueStreams;

public sealed class GetValueStreamsQuery : PaginationParams, IRequest<PagedResult<ValueStreamDto>>
{
    public string? NameFilter { get; set; }
}
