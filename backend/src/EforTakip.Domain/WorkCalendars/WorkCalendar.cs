using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.WorkCalendars;

public sealed class WorkCalendar : Entity, IAggregateRoot
{
    public string Name { get; private set; } = default!;

    private readonly List<WorkCalendarDay> _days = [];
    public IReadOnlyCollection<WorkCalendarDay> Days => _days.AsReadOnly();

    private WorkCalendar()
    {
        // EF Core
    }

    public static WorkCalendar Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("Mesai takvimi adı boş olamaz.");

        return new WorkCalendar { Name = name.Trim() };
    }

    /// <summary>Haftanın bir günü için çalışma günü olup olmadığını ve saatlerini ayarlar (upsert).</summary>
    public void SetDay(DayOfWeek dayOfWeek, bool isWorkingDay, TimeOnly? startTime, TimeOnly? endTime)
    {
        if (isWorkingDay && (startTime is null || endTime is null || endTime <= startTime))
            throw new BusinessRuleValidationException("Çalışma günü için geçerli bir başlangıç/bitiş saati gerekir.");

        var existing = _days.FirstOrDefault(d => d.DayOfWeek == dayOfWeek);
        if (existing is not null)
        {
            existing.Set(isWorkingDay, startTime, endTime);
            return;
        }

        _days.Add(WorkCalendarDay.Create(Id, dayOfWeek, isWorkingDay, startTime, endTime));
    }
}
