using EforTakip.Application.Common.Models;
using EforTakip.Application.Projects.Dtos;
using MediatR;

namespace EforTakip.Application.Projects.Queries.GetProjectTasks;

public sealed class GetProjectTasksQuery : PaginationParams, IRequest<PagedResult<ProjectTaskDto>>
{
    public Guid? ProjectId { get; set; }
}
