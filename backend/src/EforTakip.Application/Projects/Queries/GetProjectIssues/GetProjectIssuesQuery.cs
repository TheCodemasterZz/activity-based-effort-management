using EforTakip.Application.Common.Models;
using EforTakip.Application.Projects.Dtos;
using MediatR;

namespace EforTakip.Application.Projects.Queries.GetProjectIssues;

public sealed class GetProjectIssuesQuery : PaginationParams, IRequest<PagedResult<ProjectIssueDto>>
{
    public Guid? ProjectId { get; set; }
}
