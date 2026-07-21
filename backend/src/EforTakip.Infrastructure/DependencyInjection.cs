using EforTakip.Application.Directories.Ldap;
using EforTakip.Infrastructure.Ldap;
using EforTakip.Infrastructure.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace EforTakip.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ILdapService, LdapService>();
        services.AddHostedService<DirectorySyncBackgroundService>();

        return services;
    }
}
