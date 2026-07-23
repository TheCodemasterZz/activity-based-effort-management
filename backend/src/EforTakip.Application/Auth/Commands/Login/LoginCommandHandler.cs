using EforTakip.Application.Auth.Dtos;
using EforTakip.Application.Common.Exceptions;
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Domain.Directories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IApplicationDbContext db,
    IRepository<Directory> directoryRepository,
    IPasswordHasher passwordHasher,
    ILdapService ldapService,
    ITokenService tokenService)
    : IRequestHandler<LoginCommand, LoginResultDto>
{
    public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Kullanıcı adları normalize edilmiş (invariant küçük harf) olarak saklanır;
        // girdi de aynı şekilde normalize edilerek doğrudan karşılaştırılır.
        var username = request.Username.Trim().ToLowerInvariant();

        var user = await db.DirectoryUsers
            .AsNoTracking()
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        // Kullanıcının bulunamaması, pasif olması ve şifrenin yanlış olması aynı hatayı verir;
        // aksi halde saldırgan hangi kullanıcı adlarının var olduğunu öğrenebilir.
        if (user is null || !user.IsActive)
            throw new AuthenticationFailedException();

        // Dizin pasife alındıysa o dizindeki hiçbir kullanıcı giriş yapamaz — internal de AD de.
        var directory = await directoryRepository.GetByIdAsync(user.DirectoryId, cancellationToken);
        if (directory is null || !directory.IsActive)
            throw new AuthenticationFailedException();

        var authenticated = user.Source == DirectorySource.Internal
            ? VerifyInternalPassword(user, request.Password)
            : await ldapService.AuthenticateAsync(directory, user.Username, request.Password, cancellationToken);

        if (!authenticated)
            throw new AuthenticationFailedException();

        var roleIds = user.Roles.Select(r => r.RoleId).ToList();
        var roles = await db.Roles
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Where(r => roleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        var isSystemAdmin = roles.Any(r => r.IsSystemAdmin);
        var permissionKeys = roles
            .SelectMany(r => r.Permissions.Select(p => p.PermissionKey))
            .Distinct()
            .ToList();

        var (token, expiresAtUtc) = tokenService.CreateToken(new AuthenticatedUser(
            user.Id, user.Username, user.DisplayName, user.DirectoryId, user.Source,
            isSystemAdmin, permissionKeys));

        return new LoginResultDto
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Source = user.Source
        };
    }

    /// <summary>AD kullanıcısının şifresi bizde saklanmaz; her girişte dizine sorulur.</summary>
    private bool VerifyInternalPassword(DirectoryUser user, string password)
        => !string.IsNullOrEmpty(user.PasswordHash) && passwordHasher.Verify(password, user.PasswordHash);
}
