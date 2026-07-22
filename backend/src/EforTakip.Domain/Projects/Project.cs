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

    private readonly List<ProjectCustomerAssignment> _customerAssignments = [];
    public IReadOnlyCollection<ProjectCustomerAssignment> CustomerAssignments => _customerAssignments.AsReadOnly();
    public IReadOnlyCollection<Guid> CustomerIds => _customerAssignments.Select(a => a.CustomerId).ToList();

    private readonly List<ProjectEmployeeAssignment> _employeeAssignments = [];
    public IReadOnlyCollection<ProjectEmployeeAssignment> EmployeeAssignments => _employeeAssignments.AsReadOnly();
    public IReadOnlyCollection<Guid> EmployeeIds => _employeeAssignments.Select(a => a.EmployeeId).ToList();

    private Project()
    {
        // EF Core
    }

    public static Project Create(string name, string? description, DateOnly? startDate = null, DateOnly? endDate = null)
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
            HealthStatus = ProjectHealthStatus.OnTrack
        };
    }

    public void Update(string name, string? description, DateOnly? startDate, DateOnly? endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("Proje adı boş olamaz.");
        if (startDate is not null && endDate is not null && endDate < startDate)
            throw new BusinessRuleValidationException("Bitiş tarihi başlangıç tarihinden önce olamaz.");

        Name = name.Trim();
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
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

    public ProjectEmployeeAssignment AssignEmployee(Guid employeeId)
    {
        if (_employeeAssignments.Any(a => a.EmployeeId == employeeId))
            throw new BusinessRuleValidationException("Çalışan bu projeye zaten atanmış.");

        var assignment = ProjectEmployeeAssignment.Create(Id, employeeId);
        _employeeAssignments.Add(assignment);
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
