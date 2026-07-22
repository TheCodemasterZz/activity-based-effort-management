using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.WorkLogs;

public sealed class EmployeeWorkLog : Entity, IAggregateRoot
{
    public Guid EmployeeId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid ActivityL1Id { get; private set; }
    public Guid ActivityL2Id { get; private set; }
    public DateOnly WorkDate { get; private set; }
    public decimal Hours { get; private set; }
    public string Description { get; private set; } = default!;
    public Guid? ApprovalId { get; private set; }
    public bool IsApproved => ApprovalId is not null;
    public WorkLogEntryType EntryType { get; private set; }

    private EmployeeWorkLog()
    {
        // EF Core
    }

    public static EmployeeWorkLog Create(
        Guid employeeId,
        Guid projectId,
        Guid customerId,
        Guid activityL1Id,
        Guid activityL2Id,
        DateOnly workDate,
        decimal hours,
        string description,
        WorkLogEntryType entryType = WorkLogEntryType.Actual)
    {
        if (hours <= 0 || hours > 24)
            throw new BusinessRuleValidationException("Efor saati 0 ile 24 arasında olmalıdır.");

        if (string.IsNullOrWhiteSpace(description))
            throw new BusinessRuleValidationException("Açıklama boş olamaz.");

        return new EmployeeWorkLog
        {
            EmployeeId = employeeId,
            ProjectId = projectId,
            CustomerId = customerId,
            ActivityL1Id = activityL1Id,
            ActivityL2Id = activityL2Id,
            WorkDate = workDate,
            Hours = hours,
            Description = description.Trim(),
            EntryType = entryType
        };
    }

    public void Update(
        Guid employeeId,
        Guid projectId,
        Guid customerId,
        Guid activityL1Id,
        Guid activityL2Id,
        DateOnly workDate,
        decimal hours,
        string description)
    {
        if (IsApproved)
            throw new BusinessRuleValidationException("Onaylanmış bir efor kaydı değiştirilemez.");

        if (hours <= 0 || hours > 24)
            throw new BusinessRuleValidationException("Efor saati 0 ile 24 arasında olmalıdır.");

        if (string.IsNullOrWhiteSpace(description))
            throw new BusinessRuleValidationException("Açıklama boş olamaz.");

        // Gerçekleşen (Actual) kayıtlar için tarih hâlâ gelecekte olamaz — bu kural LogWork/Update
        // komut validator'larında EntryType bilgisine sahip olmadığımız Update tarafında burada,
        // Planlanan (Planned) kayıtlar için ise hiç uygulanmaz (planlamanın amacı zaten gelecek).
        if (EntryType == WorkLogEntryType.Actual && workDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BusinessRuleValidationException("Tarih gelecekte olamaz.");

        EmployeeId = employeeId;
        ProjectId = projectId;
        CustomerId = customerId;
        ActivityL1Id = activityL1Id;
        ActivityL2Id = activityL2Id;
        WorkDate = workDate;
        Hours = hours;
        Description = description.Trim();
    }

    public void EnsureDeletable()
    {
        if (IsApproved)
            throw new BusinessRuleValidationException("Onaylanmış bir efor kaydı silinemez.");
    }

    public void MarkApproved(Guid approvalId)
    {
        if (IsApproved)
            throw new BusinessRuleValidationException("Efor kaydı zaten onaylanmış.");

        ApprovalId = approvalId;
    }
}
