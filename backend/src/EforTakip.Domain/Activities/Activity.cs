using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Activities;

public sealed class Activity : Entity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid? ParentActivityId { get; private set; }

    private Activity()
    {
        // EF Core
    }

    public static Activity Create(string name, string? description, Guid? parentActivityId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("Aktivite adı boş olamaz.");

        return new Activity
        {
            Name = name.Trim(),
            Description = description,
            ParentActivityId = parentActivityId
        };
    }
}
