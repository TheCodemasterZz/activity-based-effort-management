using EforTakip.Application.Users.Dtos;
using MediatR;

namespace EforTakip.Application.Users.Queries.GetOrgChart;

public sealed record GetOrgChartQuery(Guid DirectoryId) : IRequest<OrgChartResultDto>;
