using EforTakip.Api.Extensions;
using EforTakip.Api.Middleware;
using EforTakip.Application;
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
    .AddInfrastructure()
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
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}
