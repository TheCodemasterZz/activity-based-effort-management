using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Projects;

/// <summary>Bir projeye bağlı sorun kaydı — ProjectRisk ile aynı desende, kendi başına bir
/// aggregate root, ProjectId ile gevşek bağlı.</summary>
public sealed class ProjectIssue : Entity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public ProjectIssuePriority Priority { get; private set; }
    public ProjectIssueStatus Status { get; private set; } = ProjectIssueStatus.Open;
    public Guid? OwnerUserId { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public string? Resolution { get; private set; }

    private ProjectIssue()
    {
        // EF Core
    }

    public static ProjectIssue Create(
        Guid projectId, string title, string? description, ProjectIssuePriority priority,
        Guid? ownerUserId, DateOnly? dueDate)
    {
        Validate(title);

        return new ProjectIssue
        {
            ProjectId = projectId,
            Title = title.Trim(),
            Description = description,
            Priority = priority,
            Status = ProjectIssueStatus.Open,
            OwnerUserId = ownerUserId,
            DueDate = dueDate
        };
    }

    public void Update(
        string title, string? description, ProjectIssuePriority priority,
        Guid? ownerUserId, DateOnly? dueDate, string? resolution)
    {
        Validate(title);

        Title = title.Trim();
        Description = description;
        Priority = priority;
        OwnerUserId = ownerUserId;
        DueDate = dueDate;
        Resolution = resolution;
    }

    public void SetStatus(ProjectIssueStatus status)
    {
        Status = status;
    }

    private static void Validate(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new BusinessRuleValidationException("Sorun başlığı boş olamaz.");
    }
}
