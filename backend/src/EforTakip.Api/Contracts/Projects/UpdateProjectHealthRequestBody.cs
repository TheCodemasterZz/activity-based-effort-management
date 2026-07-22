using EforTakip.Domain.Projects;

namespace EforTakip.Api.Contracts.Projects;

public sealed record UpdateProjectHealthRequestBody(ProjectHealthStatus HealthStatus);
