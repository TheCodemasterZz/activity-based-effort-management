using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Directories;

public sealed class DirectoryUserAttribute : Entity
{
    public Guid DirectoryUserId { get; private set; }
    public Guid AttributeMappingId { get; private set; }
    public string? Value { get; private set; }

    /// <summary>
    /// "Kullanıcı" tipindeki alanlar (ör. Yönetici) AD'de bir DN olarak gelir. Bu DN, aynı
    /// senkronizasyon taramasında dönen başka bir kullanıcıyla eşleşirse o kullanıcıya işaret
    /// eder; eşleşmezse null kalır ve Value alanındaki düz isim kullanılır.
    /// </summary>
    public Guid? ReferencedDirectoryUserId { get; private set; }

    private DirectoryUserAttribute()
    {
        // EF Core
    }

    public static DirectoryUserAttribute Create(
        Guid directoryUserId, Guid attributeMappingId, string? value, Guid? referencedDirectoryUserId = null)
    {
        if (attributeMappingId == Guid.Empty)
            throw new BusinessRuleValidationException("Attribute eşlemesi belirtilmelidir.");

        return new DirectoryUserAttribute
        {
            DirectoryUserId = directoryUserId,
            AttributeMappingId = attributeMappingId,
            Value = value,
            ReferencedDirectoryUserId = referencedDirectoryUserId
        };
    }

    public void SetValue(string? value, Guid? referencedDirectoryUserId = null)
    {
        Value = value;
        ReferencedDirectoryUserId = referencedDirectoryUserId;
    }
}
