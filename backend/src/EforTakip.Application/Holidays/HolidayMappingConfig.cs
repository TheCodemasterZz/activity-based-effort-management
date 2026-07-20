using EforTakip.Application.Holidays.Dtos;
using EforTakip.Domain.Holidays;
using Mapster;

namespace EforTakip.Application.Holidays;

public sealed class HolidayMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Holiday, HolidayDto>();
    }
}
