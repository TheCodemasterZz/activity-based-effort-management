using EforTakip.Application.Common.Models;
using EforTakip.Application.Holidays.Dtos;
using MediatR;

namespace EforTakip.Application.Holidays.Queries.GetHolidays;

public sealed class GetHolidaysQuery : PaginationParams, IRequest<PagedResult<HolidayDto>>
{
    public int? Year { get; set; }
}
