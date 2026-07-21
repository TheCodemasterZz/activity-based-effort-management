using EforTakip.Application.Directories.Ldap;
using EforTakip.Infrastructure.Ldap;
using Microsoft.Extensions.DependencyInjection;

namespace EforTakip.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ILdapService, LdapService>();

        return services;
    }
}
