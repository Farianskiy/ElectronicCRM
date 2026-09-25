using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Learning;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Recognition.Learning;

public sealed partial class CatalogRecognitionLearningHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<CatalogRecognitionLearningOptions> options,
    ILogger<CatalogRecognitionLearningHostedService> logger) : BackgroundService
{
    // Stable, application-specific PostgreSQL session lock, shared by all API instances.
    public const long AdvisoryLockKey = 0x4543524D4C454152;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunPassAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(options.Value.PollInterval, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown; the lock connection is disposed by RunPassAsync.
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "A background pass must log failures and retry durable input without stopping the API host.")]
    public async Task RunPassAsync(CancellationToken cancellationToken)
    {
        var feedbackCount = 0;
        var candidateCount = 0;
        var evidenceCount = 0;
        var suggestionCount = 0;
        var deferredCount = 0;
        try
        {
            await using var lockScope = scopeFactory.CreateAsyncScope();
            var db = lockScope.ServiceProvider.GetRequiredService<ElectronicDbContext>();
            var connectionString = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString())
            {
                Pooling = false,
                Multiplexing = false
            }.ConnectionString;
            // A dedicated physical session holds the lock across page commits, never a long transaction.
            // Disposing a non-pooled connection releases the lock even on cancellation or an exception.
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock(@key)", connection);
            command.Parameters.AddWithValue("key", AdvisoryLockKey);
            if (await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not true)
            {
                LogBusy();
                return;
            }

            var cutoffUtc = DateTime.UtcNow;
            LogStarted(cutoffUtc);
            // Exhaust corrections before other feedback types, including corrections on later pages.
            foreach (var type in new[] { CatalogRecognitionFeedbackType.Corrected, CatalogRecognitionFeedbackType.Accepted, CatalogRecognitionFeedbackType.Rejected })
            {
                CatalogRecognitionFeedbackCursor? cursor = null;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var aggregator = scope.ServiceProvider.GetRequiredService<ICatalogRecognitionCandidateAggregator>();
                    var result = await aggregator.AggregateAsync(type, cutoffUtc, cursor, options.Value.BatchSize, cancellationToken).ConfigureAwait(false);
                    if (result.IsFailure)
                    {
                        throw new InvalidOperationException($"Aggregation: {result.Error.Code}: {result.Error.Message}");
                    }

                    var page = result.Value;
                    feedbackCount += page.ScannedFeedbackCount;
                    evidenceCount += page.AddedEvidenceCount;
                    deferredCount += page.DeferredFeedbackCount;
                    cursor = page.NextCursor;
                    if (page.ScannedFeedbackCount < options.Value.BatchSize)
                    {
                        break;
                    }
                }
            }

            Guid? upperId;
            await using (var scope = scopeFactory.CreateAsyncScope())
            {
                upperId = await scope.ServiceProvider.GetRequiredService<ICatalogRecognitionCandidateRepository>()
                    .GetAccumulatingUpperIdAsync(cancellationToken).ConfigureAwait(false);
            }

            Guid? candidateCursor = null;
            while (upperId.HasValue)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await using var scope = scopeFactory.CreateAsyncScope();
                var promoter = scope.ServiceProvider.GetRequiredService<ICatalogRecognitionCandidateSuggestionPromoter>();
                var result = await promoter.PromoteEligibleAsync(upperId.Value, candidateCursor, options.Value.BatchSize, cancellationToken).ConfigureAwait(false);
                if (result.IsFailure)
                {
                    throw new InvalidOperationException($"Promotion: {result.Error.Code}: {result.Error.Message}");
                }

                var page = result.Value;
                candidateCount += page.ScannedCandidateCount;
                suggestionCount += page.CreatedSuggestionCount;
                deferredCount += page.DeferredCandidateCount;
                if (page.MissingAuthorCount > 0)
                {
                    LogMissingAuthor(page.MissingAuthorCount);
                }

                candidateCursor = page.NextCursor;
                if (page.ScannedCandidateCount < options.Value.BatchSize)
                {
                    break;
                }
            }

            LogCompleted(feedbackCount, candidateCount, evidenceCount, suggestionCount, deferredCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Abort the pass. All tracked state is discarded; the next cycle retries durable input.
            LogFailed(exception, feedbackCount, candidateCount, evidenceCount, suggestionCount, deferredCount);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Recognition learning pass skipped: advisory lock is held by another instance.")]
    private partial void LogBusy();

    [LoggerMessage(Level = LogLevel.Information, Message = "Recognition learning pass started, feedback cutoff {CutoffUtc}.")]
    private partial void LogStarted(DateTime cutoffUtc);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recognition learning pass completed. Feedback {FeedbackCount}, candidates {CandidateCount}, evidence added {EvidenceCount}, suggestions added {SuggestionCount}, deferred {DeferredCount}, errors 0.")]
    private partial void LogCompleted(int feedbackCount, int candidateCount, int evidenceCount, int suggestionCount, int deferredCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Recognition learning deferred {MissingAuthorCount} eligible candidates: no confirmed correction evidence with a real reviewer (missing_originating_reviewer).")]
    private partial void LogMissingAuthor(int missingAuthorCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Recognition learning pass failed; will retry with fresh scopes. Committed pages: feedback {FeedbackCount}, candidates {CandidateCount}, evidence added {EvidenceCount}, suggestions added {SuggestionCount}, deferred {DeferredCount}, errors 1.")]
    private partial void LogFailed(Exception exception, int feedbackCount, int candidateCount, int evidenceCount, int suggestionCount, int deferredCount);
}
