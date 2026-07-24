using EforTakip.Application.Common.Models;
using EforTakip.Application.Leaves.Dtos;
using MediatR;

namespace EforTakip.Application.Leaves.Queries.GetLeaves;

public sealed class GetLeavesQuery : PaginationParams, IRequest<PagedResult<LeaveDto>>
{
    public Guid? UserId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
