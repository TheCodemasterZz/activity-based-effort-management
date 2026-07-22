using EforTakip.Application.Settings.Dtos;
using EforTakip.Domain.Settings;
using Mapster;

namespace EforTakip.Application.Settings;

public sealed class ConfidenceScoreSettingsMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ConfidenceScoreSettings, ConfidenceScoreSettingsDto>();
    }
}
