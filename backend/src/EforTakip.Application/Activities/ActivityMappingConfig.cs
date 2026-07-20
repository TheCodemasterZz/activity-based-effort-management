using EforTakip.Application.Activities.Dtos;
using Mapster;
using DomainActivity = EforTakip.Domain.Activities.Activity;

namespace EforTakip.Application.Activities;

public sealed class ActivityMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<DomainActivity, ActivityDto>();
    }
}
