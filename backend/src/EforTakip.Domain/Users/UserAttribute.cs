using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Users;

public sealed class UserAttribute : Entity
{
    public Guid UserId { get; private set; }
    public Guid AttributeMappingId { get; private set; }
    public string? Value { get; private set; }

    /// <summary>
    /// "Kullanıcı" tipindeki alanlar (ör. Yönetici) AD'de bir DN olarak gelir. Bu DN, aynı
    /// senkronizasyon taramasında dönen başka bir kullanıcıyla eşleşirse o kullanıcıya işaret
    /// eder; eşleşmezse null kalır ve Value alanındaki düz isim kullanılır.
    /// </summary>
    public Guid? ReferencedUserId { get; private set; }

    private UserAttribute()
    {
        // EF Core
    }

    public static UserAttribute Create(
        Guid userId, Guid attributeMappingId, string? value, Guid? referencedUserId = null)
    {
        if (attributeMappingId == Guid.Empty)
            throw new BusinessRuleValidationException("Attribute eşlemesi belirtilmelidir.");

        return new UserAttribute
        {
            UserId = userId,
            AttributeMappingId = attributeMappingId,
            Value = value,
            ReferencedUserId = referencedUserId
        };
    }

    public void SetValue(string? value, Guid? referencedUserId = null)
    {
        Value = value;
        ReferencedUserId = referencedUserId;
    }
}
