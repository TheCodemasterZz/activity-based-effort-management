using EforTakip.Application.ValueStreams.Dtos;
using EforTakip.Domain.ValueStreams;
using Mapster;

namespace EforTakip.Application.ValueStreams;

public sealed class ValueStreamMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ValueStream, ValueStreamDto>();
        config.NewConfig<ValueStream, ValueStreamDetailDto>();
        config.NewConfig<ValueStreamStage, ValueStreamStageDto>();
    }
}
