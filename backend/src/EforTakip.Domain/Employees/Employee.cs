using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Employees;

public sealed class Employee : Entity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public string? Email { get; private set; }
    public Guid WorkCalendarId { get; private set; }

    private Employee()
    {
        // EF Core
    }

    public static Employee Create(string name, string? email, Guid workCalendarId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("Çalışan adı boş olamaz.");

        if (workCalendarId == Guid.Empty)
            throw new BusinessRuleValidationException("Çalışanın bir mesai takvimi olmalıdır.");

        return new Employee
        {
            Name = name.Trim(),
            Email = email,
            WorkCalendarId = workCalendarId
        };
    }
}
