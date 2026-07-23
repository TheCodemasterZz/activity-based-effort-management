using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Directories;

public sealed class DirectoryUserRole : Entity
{
    public Guid DirectoryUserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }

    private DirectoryUserRole()
    {
        // EF Core
    }

    public static DirectoryUserRole Create(Guid directoryUserId, Guid roleId)
    {
        if (roleId == Guid.Empty)
            throw new BusinessRuleValidationException("Atanacak rol belirtilmelidir.");

        return new DirectoryUserRole
        {
            DirectoryUserId = directoryUserId,
            RoleId = roleId,
            AssignedAtUtc = DateTime.UtcNow
        };
    }
}
