using EforTakip.Application.Projects.Dtos;
using EforTakip.Domain.Projects;
using Mapster;

namespace EforTakip.Application.Projects;

public sealed class ProjectMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Project, ProjectDto>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.HealthStatus, src => src.HealthStatus.ToString());
    }
}
