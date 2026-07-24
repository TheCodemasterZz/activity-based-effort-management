using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Roles;
using EforTakip.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Persistence.Seed;

/// <summary>
/// Sistemde hiç kullanıcı yokken ilk yönetici hesabını oluşturur. Endpoint'ler kimlik
/// doğrulama istediği için bu hesap olmadan kimse giriş yapıp kullanıcı oluşturamaz.
/// </summary>
public static class BootstrapAdminSeeder
{
    public const string InternalDirectoryName = "Internal Users";
    public const string SystemAdminRoleName = "Sistem Yöneticisi";

    public static async Task SeedAsync(
        EforTakipDbContext db,
        IPasswordHasher passwordHasher,
        string? username,
        string? password,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(cancellationToken))
            return;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Sistemde hiç kullanıcı yok ve Bootstrap:AdminUsername / Bootstrap:AdminPassword " +
                "tanımlı değil. Giriş yapılamayacak.");
            return;
        }

        var directory = await db.Directories
            .FirstOrDefaultAsync(d => d.Source == DirectorySource.Internal, cancellationToken);

        if (directory is null)
        {
            directory = Directory.CreateInternal(InternalDirectoryName, 0);
            db.Directories.Add(directory);
        }

        var adminRole = await db.Roles
            .FirstOrDefaultAsync(r => r.Name == SystemAdminRoleName, cancellationToken);

        if (adminRole is null)
        {
            adminRole = Role.Create(SystemAdminRoleName, "Sistemdeki tüm işlemlere erişebilir.", isSystemAdmin: true);
            db.Roles.Add(adminRole);
        }

        var admin = User.CreateInternal(
            directory.Id, username, null, null, username, null, passwordHasher.Hash(password));

        var assignment = admin.AssignRole(adminRole.Id);

        db.Users.Add(admin);
        if (assignment is not null)
            db.UserRoles.Add(assignment);

        await db.SaveChangesAsync(cancellationToken);

        // Şifre bilinçli olarak loglanmaz.
        logger.LogInformation("İlk yönetici hesabı oluşturuldu: {Username}", admin.Username);
    }
}
