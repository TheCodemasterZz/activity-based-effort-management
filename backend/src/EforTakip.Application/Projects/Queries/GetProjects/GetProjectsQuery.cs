using EforTakip.Application.Common.Models;
using EforTakip.Application.Projects.Dtos;
using MediatR;

namespace EforTakip.Application.Projects.Queries.GetProjects;

public sealed class GetProjectsQuery : PaginationParams, IRequest<PagedResult<ProjectDto>>
{
    public string? NameFilter { get; set; }

    /// <summary>Doluysa sadece bu çalışanın atandığı projeler döner.</summary>
    public Guid? UserId { get; set; }
}
