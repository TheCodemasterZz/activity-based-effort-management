using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EforTakip.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    public const int MinimumSigningKeyLength = 32;

    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.SigningKey))
            throw new InvalidOperationException("Jwt:SigningKey tanımlı değil. Token üretilemez.");

        if (_options.SigningKey.Length < MinimumSigningKeyLength)
            throw new InvalidOperationException(
                $"Jwt:SigningKey en az {MinimumSigningKeyLength} karakter olmalıdır.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public (string Token, DateTime ExpiresAtUtc) CreateToken(AuthenticatedUser user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("directory_id", user.DirectoryId.ToString()),
            new("directory_source", user.Source.ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            claims.Add(new Claim("display_name", user.DisplayName));

        if (user.IsSystemAdmin)
            claims.Add(new Claim("is_system_admin", "true"));

        foreach (var permissionKey in user.PermissionKeys)
            claims.Add(new Claim("permission", permissionKey));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: _signingCredentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
