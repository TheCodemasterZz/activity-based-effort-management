using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Leaves;

/// <summary>Bir çalışanın izin (leave/absence) kaydı. Tam gün (bir veya birden fazla gün süren) ya
/// da tek bir güne ait belirli saatler arası (kısmi) olabilir. Bir çalışanın birden fazla izin
/// kaydı olabilir; ancak aynı çalışanın izin dönemleri (tarih bazında) birbiriyle çakışamaz.</summary>
public sealed class Leave : Entity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public bool IsFullDay { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public string? Description { get; private set; }

    private Leave()
    {
        // EF Core
    }

    public static Leave Create(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        bool isFullDay,
        TimeOnly? startTime,
        TimeOnly? endTime,
        string? description)
    {
        if (endDate < startDate)
            throw new BusinessRuleValidationException("Bitiş tarihi başlangıç tarihinden önce olamaz.");

        if (isFullDay)
        {
            if (startTime is not null || endTime is not null)
                throw new BusinessRuleValidationException("Tam günlük izinde saat bilgisi girilemez.");
        }
        else
        {
            if (startDate != endDate)
                throw new BusinessRuleValidationException("Kısmi (saatlik) izin yalnızca tek bir günü kapsayabilir.");

            if (startTime is null || endTime is null)
                throw new BusinessRuleValidationException("Kısmi izin için başlangıç ve bitiş saati zorunludur.");

            if (startTime >= endTime)
                throw new BusinessRuleValidationException("Bitiş saati başlangıç saatinden sonra olmalıdır.");
        }

        return new Leave
        {
            UserId = userId,
            StartDate = startDate,
            EndDate = endDate,
            IsFullDay = isFullDay,
            StartTime = isFullDay ? null : startTime,
            EndTime = isFullDay ? null : endTime,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };
    }
}
