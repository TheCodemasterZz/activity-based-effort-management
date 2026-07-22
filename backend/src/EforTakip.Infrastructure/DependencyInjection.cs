using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Infrastructure.Ldap;
using EforTakip.Infrastructure.Security;
using EforTakip.Infrastructure.Sync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EforTakip.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ISettingsEncryptor, AesSettingsEncryptor>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddScoped<ILdapService, LdapService>();
        services.AddHostedService<DirectorySyncBackgroundService>();

        return services;
    }
}
