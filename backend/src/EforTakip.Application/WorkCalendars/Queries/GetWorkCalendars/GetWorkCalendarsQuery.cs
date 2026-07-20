using EforTakip.Application.Common.Models;
using EforTakip.Application.WorkCalendars.Dtos;
using MediatR;

namespace EforTakip.Application.WorkCalendars.Queries.GetWorkCalendars;

public sealed class GetWorkCalendarsQuery : PaginationParams, IRequest<PagedResult<WorkCalendarDto>>
{
}
