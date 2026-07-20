using EforTakip.Application.WorkCalendars.Dtos;
using MediatR;

namespace EforTakip.Application.WorkCalendars.Queries.GetWorkCalendarById;

public sealed record GetWorkCalendarByIdQuery(Guid WorkCalendarId) : IRequest<WorkCalendarDetailDto>;
