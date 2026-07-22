using EforTakip.Application.Projects.Dtos;
using EforTakip.Domain.Projects;
using Mapster;

namespace EforTakip.Application.Projects;

public sealed class ProjectIssueMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ProjectIssue, ProjectIssueDto>()
            .Map(dest => dest.Priority, src => src.Priority.ToString())
            .Map(dest => dest.Status, src => src.Status.ToString());
    }
}
