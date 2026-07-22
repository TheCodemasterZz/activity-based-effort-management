using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Projects;

/// <summary>Bir projeye bağlı isimli görev — Clarity PPM kartındaki kilometre taşı şeridini
/// (IsMilestone=true, sıfır süreli özel bir görev) ve ileride SPI/EVM hesaplamasını (bkz.
/// BaselineEffortHours/BaselineEndDate) mümkün kılmak için eklendi. "Task" adı bilinçli olarak
/// kullanılmadı — System.Threading.Tasks.Task ile karışmasın diye.</summary>
public sealed class ProjectTask : Entity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = default!;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public decimal EstimatedEffortHours { get; private set; }
    public ProjectTaskStatus Status { get; private set; } = ProjectTaskStatus.NotStarted;
    public bool IsMilestone { get; private set; }

    /// <summary>İlk oluşturulduğunda donar, UpdatePlan ile bir daha değişmez — EVM'in "PV,
    /// orijinal onaylanan plana dayanır" kuralı için gereken referans değer.</summary>
    public decimal BaselineEffortHours { get; private set; }
    public DateOnly BaselineEndDate { get; private set; }

    private ProjectTask()
    {
        // EF Core
    }

    public static ProjectTask Create(
        Guid projectId, string name, DateOnly startDate, DateOnly endDate, decimal estimatedEffortHours,
        bool isMilestone = false)
    {
        Validate(name, startDate, endDate, estimatedEffortHours);

        return new ProjectTask
        {
            ProjectId = projectId,
            Name = name.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            EstimatedEffortHours = estimatedEffortHours,
            IsMilestone = isMilestone,
            Status = ProjectTaskStatus.NotStarted,
            BaselineEffortHours = estimatedEffortHours,
            BaselineEndDate = endDate
        };
    }

    /// <summary>Güncel planı değiştirir — BaselineEffortHours/BaselineEndDate kasıtlı olarak
    /// buradan etkilenmez, ilk oluşturulduğu andaki değerde donmuş kalır.</summary>
    public void UpdatePlan(string name, DateOnly startDate, DateOnly endDate, decimal estimatedEffortHours, bool isMilestone)
    {
        Validate(name, startDate, endDate, estimatedEffortHours);

        Name = name.Trim();
        StartDate = startDate;
        EndDate = endDate;
        EstimatedEffortHours = estimatedEffortHours;
        IsMilestone = isMilestone;
    }

    public void SetStatus(ProjectTaskStatus status)
    {
        Status = status;
    }

    private static void Validate(string name, DateOnly startDate, DateOnly endDate, decimal estimatedEffortHours)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("Görev adı boş olamaz.");
        if (endDate < startDate)
            throw new BusinessRuleValidationException("Bitiş tarihi başlangıç tarihinden önce olamaz.");
        if (estimatedEffortHours < 0)
            throw new BusinessRuleValidationException("Tahmini efor negatif olamaz.");
    }
}
