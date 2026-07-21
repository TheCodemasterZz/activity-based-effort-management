using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Commands.SyncDirectory;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EforTakip.Infrastructure.Sync;

/// <summary>
/// Zamanlaması gelen Active Directory dizinlerini periyodik olarak senkronize eder.
/// Hassas veri (şifre, bind bilgisi) loglanmaz; yalnızca dizin adı ve sayısal özet yazılır.
/// </summary>
public sealed class DirectorySyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<DirectorySyncBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunDueSyncsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Zamanlanmış dizin senkronizasyonu turu başarısız oldu.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunDueSyncsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        var nowUtc = DateTime.UtcNow;

        var candidates = await db.Directories
            .AsNoTracking()
            .Where(d => d.IsActive)
            .ToListAsync(cancellationToken);

        var dueDirectoryIds = candidates
            .Where(d => d.IsSyncDue(nowUtc))
            .Select(d => d.Id)
            .ToList();

        foreach (var directoryId in dueDirectoryIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await mediator.Send(new SyncDirectoryCommand(directoryId), cancellationToken);
                logger.LogInformation(
                    "Dizin senkronizasyonu tamamlandı: {DirectoryName} — {Added} eklendi, {Updated} güncellendi, {Deactivated} pasife alındı.",
                    result.DirectoryName, result.Added, result.Updated, result.Deactivated);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Tek bir dizinin hatası diğerlerini ve zamanlayıcıyı durdurmamalı.
                logger.LogError(ex, "Dizin senkronizasyonu başarısız oldu: {DirectoryId}", directoryId);
            }
        }
    }
}
