using EforTakip.Application.Projects.Dtos;
using EforTakip.Domain.Projects;
using Mapster;

namespace EforTakip.Application.Projects;

public sealed class ProjectRiskMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ProjectRisk, ProjectRiskDto>()
            .Map(dest => dest.Status, src => src.Status.ToString());
    }
}
