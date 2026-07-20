namespace EforTakip.Application.Projects.Dtos;

public sealed class ProjectDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string Status { get; init; } = default!;
    public IReadOnlyCollection<CustomerSummaryDto> Customers { get; init; } = [];
    public IReadOnlyCollection<EmployeeSummaryDto> Employees { get; init; } = [];
}
