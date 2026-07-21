using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Directories;

public sealed class DirectoryUserAttribute : Entity
{
    public Guid DirectoryUserId { get; private set; }
    public Guid AttributeMappingId { get; private set; }
    public string? Value { get; private set; }

    private DirectoryUserAttribute()
    {
        // EF Core
    }

    public static DirectoryUserAttribute Create(Guid directoryUserId, Guid attributeMappingId, string? value)
    {
        if (attributeMappingId == Guid.Empty)
            throw new BusinessRuleValidationException("Attribute eşlemesi belirtilmelidir.");

        return new DirectoryUserAttribute
        {
            DirectoryUserId = directoryUserId,
            AttributeMappingId = attributeMappingId,
            Value = value
        };
    }

    public void SetValue(string? value) => Value = value;
}
