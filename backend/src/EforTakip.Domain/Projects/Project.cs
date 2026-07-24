using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Projects;

public sealed class Project : Entity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public ProjectStatus Status { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public ProjectHealthStatus HealthStatus { get; private set; } = ProjectHealthStatus.OnTrack;
    public string? Sponsor { get; private set; }
    public Guid? ProjectManagerUserId { get; private set; }
    public ProjectPriority Priority { get; private set; } = ProjectPriority.Medium;
    public string? StrategicGoal { get; private set; }

    private readonly List<ProjectCustomerAssignment> _customerAssignments = [];
    public IReadOnlyCollection<ProjectCustomerAssignment> CustomerAssignments => _customerAssignments.AsReadOnly();
    public IReadOnlyCollection<Guid> CustomerIds => _customerAssignments.Select(a => a.CustomerId).ToList();

    private readonly List<ProjectUserAssignment> _userAssignments = [];
    public IReadOnlyCollection<ProjectUserAssignment> UserAssignments => _userAssignments.AsReadOnly();
    public IReadOnlyCollection<Guid> UserIds => _userAssignments.Select(a => a.UserId).ToList();

    private Project()
    {
        // EF Core
    }

    public static Project Create(
        string name,
        string? description,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? sponsor = null,
        Guid? projectManagerUserId = null,
        ProjectPriority priority = ProjectPriority.Medium,
        string? strategicGoal = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("Proje adı boş olamaz.");
        if (startDate is not null && endDate is not null && endDate < startDate)
            throw new BusinessRuleValidationException("Bitiş tarihi başlangıç tarihinden önce olamaz.");

        return new Project
        {
            Name = name.Trim(),
            Description = description,
            Status = ProjectStatus.Active,
            StartDate = startDate,
            EndDate = endDate,
            HealthStatus = ProjectHealthStatus.OnTrack,
            Sponsor = sponsor,
            ProjectManagerUserId = projectManagerUserId,
            Priority = priority,
            StrategicGoal = strategicGoal
        };
    }

    public void Update(
        string name,
        string? description,
        DateOnly? startDate,
        DateOnly? endDate,
        string? sponsor,
        Guid? projectManagerUserId,
        ProjectPriority priority,
        string? strategicGoal)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("Proje adı boş olamaz.");
        if (startDate is not null && endDate is not null && endDate < startDate)
            throw new BusinessRuleValidationException("Bitiş tarihi başlangıç tarihinden önce olamaz.");

        Name = name.Trim();
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        Sponsor = sponsor;
        ProjectManagerUserId = projectManagerUserId;
        Priority = priority;
        StrategicGoal = strategicGoal;
    }

    /// <summary>Proje yöneticisinin elle güncellediği genel sağlık rozeti (On Track/At Risk/Needs
    /// Help) — diğer alanlardan bağımsız, daha sık değişebileceği için ayrı bir komutla yönetilir.</summary>
    public void SetHealthStatus(ProjectHealthStatus status)
    {
        HealthStatus = status;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new BusinessRuleValidationException("Proje zaten pasif durumda.");

        IsActive = false;
    }

    public ProjectCustomerAssignment AssignCustomer(Guid customerId)
    {
        if (_customerAssignments.Any(a => a.CustomerId == customerId))
            throw new BusinessRuleValidationException("Müşteri bu projeye zaten atanmış.");

        var assignment = ProjectCustomerAssignment.Create(Id, customerId);
        _customerAssignments.Add(assignment);
        return assignment;
    }

    public ProjectUserAssignment AssignUser(Guid userId)
    {
        if (_userAssignments.Any(a => a.UserId == userId))
            throw new BusinessRuleValidationException("Çalışan bu projeye zaten atanmış.");

        var assignment = ProjectUserAssignment.Create(Id, userId);
        _userAssignments.Add(assignment);
        return assignment;
    }

    public void Complete()
    {
        if (Status != ProjectStatus.Active)
            throw new BusinessRuleValidationException("Yalnızca aktif projeler tamamlanabilir.");

        Status = ProjectStatus.Completed;
    }

    public void Cancel()
    {
        if (Status == ProjectStatus.Completed)
            throw new BusinessRuleValidationException("Tamamlanmış proje iptal edilemez.");

        Status = ProjectStatus.Cancelled;
    }
}
