using EforTakip.Application.Common.Models;

namespace EforTakip.Application.Common.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(AuthenticatedUser user);
}
