using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.WorkLogApprovals;

/// <summary>Bir çalışanın belirli bir haftadaki (Pazartesi–Pazar) tüm efor kayıtlarının toplu
/// olarak onaylandığını temsil eder. Onaylanan EmployeeWorkLog kayıtları bu kaydın Id'sini
/// ApprovalId olarak taşır ve bu sayede değiştirilemez/silinemez hale gelir. Onay her zaman tam
/// bir hafta kapsar — haftanın bir kısmı onaylı bir kısmı onaysız olamaz (ör. Pazartesi onaylı,
/// Salı onaysız, Çarşamba onaylı gibi parçalı bir durum yapısal olarak imkansızdır).</summary>
public sealed class WorkLogApproval : Entity, IAggregateRoot
{
    public Guid EmployeeId { get; private set; }
    public ApprovalPeriodType PeriodType { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public string Description { get; private set; } = default!;
    public DateTime ApprovedAtUtc { get; private set; }

    private WorkLogApproval()
    {
        // EF Core
    }

    public static WorkLogApproval Create(
        Guid employeeId, ApprovalPeriodType periodType, DateOnly periodStart, DateOnly periodEnd, string description)
    {
        if (periodStart.DayOfWeek != DayOfWeek.Monday)
            throw new BusinessRuleValidationException("Onay dönemi Pazartesi gününden başlamalıdır.");

        if (periodEnd != periodStart.AddDays(6))
            throw new BusinessRuleValidationException("Onay dönemi tam bir hafta (Pazartesi–Pazar) olmalıdır.");

        if (string.IsNullOrWhiteSpace(description))
            throw new BusinessRuleValidationException("Onay açıklaması zorunludur.");

        return new WorkLogApproval
        {
            EmployeeId = employeeId,
            PeriodType = periodType,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Description = description.Trim(),
            ApprovedAtUtc = DateTime.UtcNow
        };
    }
}
