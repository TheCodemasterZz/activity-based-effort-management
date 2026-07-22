using EforTakip.Application.Directories.Dtos;
using MediatR;

namespace EforTakip.Application.Directories.Queries.GetOrgChart;

public sealed record GetOrgChartQuery(Guid DirectoryId) : IRequest<OrgChartResultDto>;
