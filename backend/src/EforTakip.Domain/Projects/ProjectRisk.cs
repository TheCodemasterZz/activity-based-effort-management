using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Projects;

/// <summary>Bir projeye bağlı risk kaydı — ProjectTask gibi Project'in yanında ama kendi
/// başına bir aggregate root, ProjectId ile gevşek bağlı. Skor (Probability * Impact)
/// bilinçli olarak DB'de saklanmaz; SPI'da olduğu gibi Dto/frontend'de hesaplanır.</summary>
public sealed class ProjectRisk : Entity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public int Probability { get; private set; }
    public int Impact { get; private set; }
    public ProjectRiskStatus Status { get; private set; } = ProjectRiskStatus.Open;
    public string? MitigationPlan { get; private set; }
    public Guid? OwnerEmployeeId { get; private set; }
    public DateOnly IdentifiedDate { get; private set; }

    private ProjectRisk()
    {
        // EF Core
    }

    public static ProjectRisk Create(
        Guid projectId, string title, string? description, int probability, int impact,
        string? mitigationPlan, Guid? ownerEmployeeId, DateOnly identifiedDate)
    {
        Validate(title, probability, impact);

        return new ProjectRisk
        {
            ProjectId = projectId,
            Title = title.Trim(),
            Description = description,
            Probability = probability,
            Impact = impact,
            Status = ProjectRiskStatus.Open,
            MitigationPlan = mitigationPlan,
            OwnerEmployeeId = ownerEmployeeId,
            IdentifiedDate = identifiedDate
        };
    }

    public void Update(
        string title, string? description, int probability, int impact,
        string? mitigationPlan, Guid? ownerEmployeeId, DateOnly identifiedDate)
    {
        Validate(title, probability, impact);

        Title = title.Trim();
        Description = description;
        Probability = probability;
        Impact = impact;
        MitigationPlan = mitigationPlan;
        OwnerEmployeeId = ownerEmployeeId;
        IdentifiedDate = identifiedDate;
    }

    public void SetStatus(ProjectRiskStatus status)
    {
        Status = status;
    }

    private static void Validate(string title, int probability, int impact)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new BusinessRuleValidationException("Risk başlığı boş olamaz.");
        if (probability is < 1 or > 5)
            throw new BusinessRuleValidationException("Olasılık 1 ile 5 arasında olmalıdır.");
        if (impact is < 1 or > 5)
            throw new BusinessRuleValidationException("Etki 1 ile 5 arasında olmalıdır.");
    }
}
