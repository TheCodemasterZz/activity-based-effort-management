using Microsoft.Extensions.DependencyInjection;

namespace EforTakip.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Dış sistem entegrasyonları (e-posta, dosya depolama, mesajlaşma vb.) gerçek
        // bir ihtiyaç doğduğunda burada kaydedilir.
        return services;
    }
}
