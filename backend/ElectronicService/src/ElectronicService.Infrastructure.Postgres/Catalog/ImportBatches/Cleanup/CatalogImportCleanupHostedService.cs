using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElectronicService.Infrastructure.Postgres.Catalog.ImportBatches.Cleanup;

public sealed partial class CatalogImportCleanupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CatalogImportCleanupOptions _options;
    private readonly ILogger<CatalogImportCleanupHostedService> _logger;

    public CatalogImportCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<CatalogImportCleanupOptions> options,
        ILogger<CatalogImportCleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            LogCleanupDisabled();

            return;
        }

        try
        {
            if (_options.InitialDelayMinutes > 0)
            {
                await Task.Delay(
                        TimeSpan.FromMinutes(_options.InitialDelayMinutes),
                        stoppingToken)
                    .ConfigureAwait(false);
            }

            await RunCleanupAsync(stoppingToken)
                .ConfigureAwait(false);

            using var timer = new PeriodicTimer(
                TimeSpan.FromHours(_options.IntervalHours));

            while (await timer
                       .WaitForNextTickAsync(stoppingToken)
                       .ConfigureAwait(false))
            {
                await RunCleanupAsync(stoppingToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            LogCleanupStopped();
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        var cutoffUtc = DateTime.UtcNow.AddDays(
            -_options.RetentionDays);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var cleanupService = scope.ServiceProvider
                .GetRequiredService<CatalogImportCleanupService>();

            var deletedCount = await cleanupService
                .DeleteExpiredAsync(
                    cutoffUtc,
                    _options.BatchSize,
                    cancellationToken)
                .ConfigureAwait(false);

            LogCleanupCompleted(
                deletedCount,
                cutoffUtc);
        }
        catch (DbException exception)
        {
            LogCleanupDatabaseFailure(
                cutoffUtc,
                exception);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Catalog import cleanup is disabled.")]
    private partial void LogCleanupDisabled();

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Catalog import cleanup deleted {DeletedCount} expired batches with cutoff {CutoffUtc}.")]
    private partial void LogCleanupCompleted(
        int deletedCount,
        DateTime cutoffUtc);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Catalog import cleanup failed for cutoff {CutoffUtc}.")]
    private partial void LogCleanupDatabaseFailure(
        DateTime cutoffUtc,
        Exception exception);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Catalog import cleanup stopped.")]
    private partial void LogCleanupStopped();
}