using EforTakip.Api.Extensions;
using EforTakip.Api.Middleware;
using EforTakip.Application;
using EforTakip.Application.Common.Interfaces;
using EforTakip.Infrastructure;
using EforTakip.Persistence;
using EforTakip.Persistence.Seed;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// User secrets normalde sadece "Development" ortamında otomatik yüklenir; gerçek DB ile
// test için ayrı bir ortam (RealDb) kullanıldığında da yüklenmesi gerekir.
if (!builder.Environment.IsDevelopment())
    builder.Configuration.AddUserSecrets<Program>();

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
    // TestDataSeeder buradan ÇAĞRILMAZ: artık sahte User kayıtları ürettiği için
    // BootstrapAdminSeeder'ın "hiç kullanıcı yoksa" koşulunu bozar (admin hiç oluşmazdı).
    // Sahte veri, admin oluştuktan sonra aşağıda seed edilir.
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

    if (builder.Configuration.GetValue<bool>("UseTestMode"))
        await TestDataSeeder.SeedAsync(db);
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
