using EforTakip.Application.WorkCalendars.Dtos;
using EforTakip.Domain.WorkCalendars;
using Mapster;

namespace EforTakip.Application.WorkCalendars;

public sealed class WorkCalendarMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<WorkCalendar, WorkCalendarDto>();
        config.NewConfig<WorkCalendar, WorkCalendarDetailDto>();
        config.NewConfig<WorkCalendarDay, WorkCalendarDayDto>();
    }
}
