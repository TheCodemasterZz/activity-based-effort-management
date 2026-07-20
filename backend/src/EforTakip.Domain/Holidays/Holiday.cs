using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Holidays;

public sealed class Holiday : Entity, IAggregateRoot
{
    public DateOnly Date { get; private set; }
    public string Name { get; private set; } = default!;

    private Holiday()
    {
        // EF Core
    }

    public static Holiday Create(DateOnly date, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("Tatil adı boş olamaz.");

        return new Holiday
        {
            Date = date,
            Name = name.Trim()
        };
    }
}
