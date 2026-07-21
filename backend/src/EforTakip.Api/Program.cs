using EforTakip.Api.Extensions;
using EforTakip.Api.Middleware;
using EforTakip.Application;
using EforTakip.Application.Common.Interfaces;
using EforTakip.Infrastructure;
using EforTakip.Persistence;
using EforTakip.Persistence.Seed;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPersistence(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("UseTestMode"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EforTakipDbContext>();
    await db.Database.EnsureCreatedAsync();
    await TestDataSeeder.SeedAsync(db);
}

// Endpoint'ler kimlik doğrulama istediğinden, sistemde hiç kullanıcı yoksa kimse giriş
// yapıp ilk hesabı oluşturamaz — bu yüzden açılışta bir yönetici hesabı hazırlanır.
using (var bootstrapScope = app.Services.CreateScope())
{
    var db = bootstrapScope.ServiceProvider.GetRequiredService<EforTakipDbContext>();
    var passwordHasher = bootstrapScope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = bootstrapScope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger(nameof(BootstrapAdminSeeder));

    await BootstrapAdminSeeder.SeedAsync(
        db,
        passwordHasher,
        builder.Configuration["Bootstrap:AdminUsername"],
        builder.Configuration["Bootstrap:AdminPassword"],
        logger,
        CancellationToken.None);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseHttpsRedirection();
app.UseCors(ApiServiceCollectionExtensions.FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program
{
}
