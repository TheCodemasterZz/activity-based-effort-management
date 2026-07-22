using EforTakip.Application.Common.Models;
using EforTakip.Application.Projects.Dtos;
using MediatR;

namespace EforTakip.Application.Projects.Queries.GetProjectRisks;

public sealed class GetProjectRisksQuery : PaginationParams, IRequest<PagedResult<ProjectRiskDto>>
{
    public Guid? ProjectId { get; set; }
}
