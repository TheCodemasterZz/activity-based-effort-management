using EforTakip.Application.Projects.Dtos;
using MediatR;

namespace EforTakip.Application.Projects.Queries.GetProjectById;

public sealed record GetProjectByIdQuery(Guid ProjectId) : IRequest<ProjectDetailDto>;
