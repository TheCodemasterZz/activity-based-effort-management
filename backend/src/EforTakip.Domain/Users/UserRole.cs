using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Users;

public sealed class UserRole : Entity
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }

    private UserRole()
    {
        // EF Core
    }

    public static UserRole Create(Guid userId, Guid roleId)
    {
        if (roleId == Guid.Empty)
            throw new BusinessRuleValidationException("Atanacak rol belirtilmelidir.");

        return new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAtUtc = DateTime.UtcNow
        };
    }
}
