using EforTakip.Application.Projects.Dtos;
using EforTakip.Domain.Projects;
using Mapster;

namespace EforTakip.Application.Projects;

public sealed class ProjectTaskMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ProjectTask, ProjectTaskDto>()
            .Map(dest => dest.Status, src => src.Status.ToString());
    }
}
