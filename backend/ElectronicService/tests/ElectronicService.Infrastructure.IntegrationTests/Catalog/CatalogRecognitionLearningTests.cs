using System.Text.Json;
using ElectronicService.Core;
using ElectronicService.Core.Catalog.Recognition.Evaluation;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;
using ElectronicService.Infrastructure.Postgres;
using Microsoft.Extensions.Configuration;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Learning;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Infrastructure.IntegrationTests.Data;
using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using ElectronicService.Infrastructure.Postgres.Catalog.ImportBatches;
using ElectronicService.Infrastructure.Postgres.Catalog.Recognition.Learning;
using ElectronicService.Infrastructure.Postgres.Catalog.Repositories;
using ElectronicService.Infrastructure.Postgres.Data;
using ElectronicService.TestCommon;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed class CatalogRecognitionLearningTests(PostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task StartupAndRestartDrainBothQueuesPastDeferredPagesWithoutDuplicatingEvidence()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var (graph, reviewer) = await SeedGraphAsync(db, ct);
        var tiedTime = DateTime.UtcNow.AddMinutes(-1);
        for (var i = 1; i <= 501; i++)
        {
            var deferred = Feedback(graph, reviewer, $"missing-{i}", CatalogRecognitionFeedbackType.Accepted);
            db.CatalogRecognitionFeedbackEntries.Add(deferred);
            db.Entry(deferred).Property(x => x.FinalizedAtUtc).CurrentValue = tiedTime;
            db.Entry(deferred).Property(x => x.CreatedAtUtc).CurrentValue = tiedTime;
            db.Entry(deferred).Property(x => x.Id).CurrentValue = new Guid($"00000001-0000-0000-0000-{i:000000000000}");
            db.CatalogRecognitionFeedbackEntries.Add(Feedback(graph, reviewer, $"unready-{i}", CatalogRecognitionFeedbackType.Corrected));
            var unready = Candidate(graph, $"unready-{i}");
            db.CatalogRecognitionCandidates.Add(unready);
            db.Entry(unready).Property(x => x.Id).CurrentValue = new Guid($"00000001-0000-0001-0000-{i:000000000000}");
        }

        // Accepted is behind >500 deferred rows, with exactly the same finalized timestamp.
        var accepted = Feedback(graph, reviewer, "ready", CatalogRecognitionFeedbackType.Accepted);
        db.CatalogRecognitionFeedbackEntries.Add(accepted);
        db.Entry(accepted).Property(x => x.FinalizedAtUtc).CurrentValue = tiedTime;
        db.Entry(accepted).Property(x => x.CreatedAtUtc).CurrentValue = tiedTime;
        db.Entry(accepted).Property(x => x.Id).CurrentValue = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff");
        for (var i = 0; i < 3; i++)
        {
            db.CatalogRecognitionFeedbackEntries.Add(Feedback(graph, reviewer, "ready", CatalogRecognitionFeedbackType.Corrected, $"product-{i}"));
        }

        var orphan = Candidate(graph, "no-origin");
        Assert.True(orphan.AddEvidence(CatalogRecognitionFeedbackType.Corrected, DateTime.UtcNow, true).IsSuccess);
        Assert.True(orphan.AddEvidence(CatalogRecognitionFeedbackType.Corrected, DateTime.UtcNow, true).IsSuccess);
        db.CatalogRecognitionCandidates.Add(orphan);
        await db.SaveChangesAsync(ct);
        await using var provider = CreateProvider();
        var log = new LearningLog();
        using (var service = Service(provider, log))
        {
            await service.StartAsync(ct);
            await log.Completed.Task.WaitAsync(TimeSpan.FromSeconds(45), ct);
            await service.StopAsync(ct);
        }

        Assert.Empty(log.Errors);
        Assert.Contains(log.Messages, x => x.Contains("missing_originating_reviewer", StringComparison.Ordinal));
        await AssertReadyAsync(graph, reviewer, 4, ct);
        using (var restarted = Service(provider, new LearningLog()))
        {
            await restarted.RunPassAsync(ct);
        }

        await AssertReadyAsync(graph, reviewer, 4, ct);
        // Deferred feedback must be revisited in the next full pass after a correction appears.
        db.CatalogRecognitionFeedbackEntries.Add(Feedback(graph, reviewer, "missing-1", CatalogRecognitionFeedbackType.Corrected));
        await db.SaveChangesAsync(ct);
        using var next = Service(provider, new LearningLog());
        await next.RunPassAsync(ct);
        await using var check = fixture.CreateDbContext();
        var revisited = await check.CatalogRecognitionCandidates.SingleAsync(x => x.ManufacturerId == graph.Manufacturer.Id && x.Phrase == "missing-1", ct);
        Assert.Equal(2, revisited.OccurrenceCount);
    }

    [Fact]
    public async Task CompetingInstancesSkipLockedPassAndReleaseLockAfterCancellation()
    {
        var ct = TestContext.Current.CancellationToken;
        var blocker = new BlockingAggregator();
        await using var firstProvider = CreateProvider(aggregator: blocker);
        await using var secondProvider = CreateProvider(aggregator: blocker);
        using var first = Service(firstProvider, new LearningLog());
        var secondLog = new LearningLog();
        using var second = Service(secondProvider, secondLog);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var running = first.RunPassAsync(stop.Token);
        await blocker.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15), ct);
        await second.RunPassAsync(ct);
        Assert.Equal(1, blocker.CallCount);
        Assert.Contains(secondLog.Messages, x => x.Contains("skipped", StringComparison.Ordinal));
        await stop.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
        await using var cleanProvider = CreateProvider();
        var cleanLog = new LearningLog();
        using var clean = Service(cleanProvider, cleanLog);
        await clean.RunPassAsync(ct);
        Assert.Empty(cleanLog.Errors);
        Assert.True(cleanLog.Completed.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task ImportCommitsFeedbackWithoutLearningAndFailedBackgroundSaveRetriesWithCleanContext()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var (graph, reviewer) = await SeedGraphAsync(db, ct);
        var batch = CatalogImportBatch.Create(reviewer, "learning.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", [1]).Value;
        var article = $"LEARNING-{Guid.NewGuid():N}";
        var data = new CatalogImportNormalizedRowData("learning product", article, graph.Manufacturer.Name, 10m, 1,
            new Dictionary<string, string>(StringComparer.Ordinal) { [graph.Definition.Id.ToString()] = "16" },
            ManufacturerId: graph.Manufacturer.Id, ProductTypeId: graph.ProductType.Id);
        var row = CatalogImportRow.Create(batch.Id, 2, CatalogImportRowStatus.Valid, "{}", JsonSerializer.Serialize(data, JsonOptions), "[]", "[]").Value;
        Assert.True(batch.RegisterAnalysisResult(1, 1, 0, false).IsSuccess);
        db.CatalogImportBatches.Add(batch);
        db.CatalogImportRows.Add(row);
        var feedback = Feedback(graph, reviewer, "imported", CatalogRecognitionFeedbackType.Corrected, finalize: false, batchId: batch.Id, rowId: row.Id);
        db.CatalogRecognitionFeedbackEntries.Add(feedback);
        await db.SaveChangesAsync(ct);
        // No aggregator or promoter is registered or passed to the applier.
        var applier = new CatalogImportBatchApplier(db,
            new CatalogImportRecognitionFeedbackFinalizer(new CatalogRecognitionFeedbackRepository(db)),
            NullLogger<CatalogImportBatchApplier>.Instance);
        var applied = await applier.ApplyAsync(batch, reviewer, UserType.Technical, ct);
        Assert.True(applied.IsSuccess, applied.IsFailure ? applied.Error.Message : null);
        Assert.True(await db.Products.AnyAsync(x => x.Article.Value == article, ct));
        Assert.True(feedback.IsFinalized && feedback.IsTrainingEligible);
        Assert.False(await db.CatalogRecognitionCandidateEvidenceEntries.AnyAsync(x => x.FeedbackId == feedback.Id, ct));

        var failure = new FailEvidenceSaveOnce();
        await using var provider = CreateProvider(failure);
        var log = new LearningLog();
        using var service = Service(provider, log);
        await service.RunPassAsync(ct);
        Assert.Single(log.Errors);
        Assert.False(log.Completed.Task.IsCompleted);
        await using (var afterFailure = fixture.CreateDbContext())
        {
            Assert.Equal(CatalogImportBatchStatus.Applied, (await afterFailure.CatalogImportBatches.SingleAsync(x => x.Id == batch.Id, ct)).Status);
            Assert.False(await afterFailure.CatalogRecognitionCandidateEvidenceEntries.AnyAsync(x => x.FeedbackId == feedback.Id, ct));
        }

        await service.RunPassAsync(ct);
        Assert.True(log.Completed.Task.IsCompletedSuccessfully);
        Assert.NotEqual(failure.FailedContext, failure.SuccessfulContext);
        await using var check = fixture.CreateDbContext();
        Assert.Equal(1, await check.CatalogRecognitionCandidateEvidenceEntries.CountAsync(x => x.FeedbackId == feedback.Id, ct));
        Assert.Equal(1, (await check.CatalogRecognitionCandidates.SingleAsync(x => x.ManufacturerId == graph.Manufacturer.Id, ct)).OccurrenceCount);
    }

    private async Task AssertReadyAsync(CatalogProductGraph graph, Guid reviewer, int occurrences, CancellationToken ct)
    {
        await using var check = fixture.CreateDbContext();
        var ready = await check.CatalogRecognitionCandidates.SingleAsync(x => x.ManufacturerId == graph.Manufacturer.Id && x.Phrase == "ready", ct);
        Assert.Equal(occurrences, ready.OccurrenceCount);
        Assert.Equal(occurrences, await check.CatalogRecognitionCandidateEvidenceEntries.CountAsync(x => x.CandidateId == ready.Id, ct));
        Assert.Equal(3, ready.CorrectedCount);
        Assert.Equal(1, ready.AcceptedCount);
        var suggestion = await check.CatalogAssistantDictionarySuggestions.SingleAsync(x => x.Id == ready.SuggestionId, ct);
        Assert.Equal(reviewer, suggestion.CreatedByUserId);
        Assert.True(suggestion.GeneratedAutomatically);
    }

    private static async Task<(CatalogProductGraph Graph, Guid Reviewer)> SeedGraphAsync(ElectronicDbContext db, CancellationToken ct)
    {
        var graph = PostgreSqlTestDataFactory.CreateCatalogProductGraph();
        var user = TestDataFactory.CreateTechnicalUser(email: $"learning-{Guid.NewGuid():N}@example.com");
        db.Users.Add(user);
        db.Manufacturers.Add(graph.Manufacturer);
        db.CharacteristicDefinitions.Add(graph.Definition);
        db.ProductTypes.Add(graph.ProductType);
        await db.SaveChangesAsync(ct);
        return (graph, user.Id);
    }

    private static CatalogRecognitionCandidate Candidate(CatalogProductGraph graph, string phrase)
    {
        return CatalogRecognitionCandidate.Create(phrase, phrase.ToUpperInvariant(), graph.Manufacturer.Id,
            graph.ProductType.Id, graph.ProductType.Code, graph.Definition.Id, graph.Definition.Code,
            "16", CatalogRecognitionFeedbackType.Corrected, DateTime.UtcNow).Value;
    }

    private static CatalogRecognitionFeedback Feedback(CatalogProductGraph graph, Guid reviewer, string phrase,
        CatalogRecognitionFeedbackType type, string product = "product", bool finalize = true, Guid? batchId = null, Guid? rowId = null)
    {
        var result = CatalogRecognitionFeedback.Create(product, product.ToUpperInvariant(), graph.Manufacturer.Id,
            graph.ProductType.Id, graph.ProductType.Code, graph.Definition.Id, graph.Definition.Code,
            phrase, type == CatalogRecognitionFeedbackType.Corrected ? "10" : "16", 0.9m, "test", null, null,
            type, "16", null, null, null, batchId, rowId);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        if (finalize)
        {
            Assert.True(result.Value.Finalize(CatalogRecognitionLabelQuality.Strong, reviewer, "Technical", true).IsSuccess);
        }

        return result.Value;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExclusionInvalidatesApprovalRevisionAndPreservesAlreadyApprovedTerm(bool approveBeforeSecondExclusion)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var (graph, reviewer) = await SeedGraphAsync(db, ct);
        var sources = Enumerable.Range(0, 4).Select(i => Feedback(graph, reviewer, "provenance-phrase", CatalogRecognitionFeedbackType.Corrected, $"product-{i}")).ToArray();
        db.CatalogRecognitionFeedbackEntries.AddRange(sources);
        await db.SaveChangesAsync(ct);
        await using var provider = CreateProvider();
        using var worker = Service(provider, new LearningLog());
        await worker.RunPassAsync(ct);
        var candidate = await db.CatalogRecognitionCandidates.AsNoTracking().SingleAsync(x => x.ManufacturerId == graph.Manufacturer.Id && x.Phrase == "provenance-phrase", ct);
        Assert.NotNull(candidate.SuggestionId);
        var provenance = Provenance(db, reviewer);
        var preview = (await provenance.ReadSuggestionAsync(candidate.SuggestionId.Value, ct)).Value;
        Assert.True(preview.SufficientEvidence);
        Assert.True((await provenance.ExcludeAsync(sources[0].Id, "Первое исключение", ct)).IsSuccess);
        var command = new ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion.ApproveCatalogAssistantDictionarySuggestionCommand(
            candidate.SuggestionId.Value, "provenance-phrase", "Characteristic", graph.Definition.Code, "16", graph.ProductType.Code, 100, null, preview.Revision);
        await using var approvalProvider = ApprovalProvider(db, reviewer);
        var approval = approvalProvider.GetRequiredService<ApproveCatalogAssistantDictionarySuggestionCommandHandler>();
        Assert.Equal("training.conflict", (await approval.Handle(command, ct)).Error.Code);
        var fresh = (await provenance.ReadSuggestionAsync(candidate.SuggestionId.Value, ct)).Value;
        Assert.True(fresh.SufficientEvidence);
        if (!approveBeforeSecondExclusion)
        {
            Assert.True((await provenance.ExcludeAsync(sources[1].Id, "Недостаточно оснований", ct)).IsSuccess);
            var insufficient = (await provenance.ReadSuggestionAsync(candidate.SuggestionId.Value, ct)).Value;
            Assert.False(insufficient.SufficientEvidence);
            Assert.Equal("training.conflict", (await approval.Handle(command with { EvidenceRevision = insufficient.Revision }, ct)).Error.Code);
            return;
        }
        await AddControlAsync(db, graph, reviewer, "provenance-phrase", ct);
        var evaluatedCommand = command with { EvidenceRevision = fresh.Revision };
        var report = await approvalProvider.GetRequiredService<IDictionaryEvaluationReports>().CreateAsync(evaluatedCommand, ct);
        Assert.True(report.IsSuccess, report.IsFailure ? report.Error.Message : null);
        var approved = await approval.Handle(evaluatedCommand with { EvaluationReportId = report.Value, Confirmed = true }, ct);
        Assert.True(approved.IsSuccess, approved.IsFailure ? approved.Error.Message : null);
        var impact = await provenance.ExcludeAsync(sources[1].Id, "После выпуска", ct);
        Assert.True(impact.IsSuccess);
        var affected = Assert.Single(impact.Value.Candidates.Items);
        Assert.Equal("Approved", affected.SuggestionStatus);
        Assert.NotNull(affected.DictionaryTermId);
        Assert.True(affected.NeedsReview);
        Assert.True(await db.CatalogDictionaryTerms.AnyAsync(x => x.Id == affected.DictionaryTermId && x.Status == ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTermStatus.Approved, ct));
        Assert.Equal(4, await db.CatalogRecognitionCandidateEvidenceEntries.CountAsync(x => x.CandidateId == candidate.Id, ct));
    }

    [Fact]
    public async Task ConcurrentExclusionAndLearningCannotRestoreExcludedEvidence()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var (graph, reviewer) = await SeedGraphAsync(db, ct);
        var sources = Enumerable.Range(0, 3).Select(i => Feedback(graph, reviewer, "race-phrase", CatalogRecognitionFeedbackType.Corrected, $"race-{i}")).ToArray();
        db.CatalogRecognitionFeedbackEntries.AddRange(sources);
        await db.SaveChangesAsync(ct);
        await using var provider = CreateProvider();
        using var worker = Service(provider, new LearningLog());
        await using var exclusionDb = fixture.CreateDbContext();
        var exclusion = Provenance(exclusionDb, reviewer).ExcludeAsync(sources[0].Id, "Конкурентное исключение", ct);
        await Task.WhenAll(exclusion, worker.RunPassAsync(ct));
        Assert.True((await exclusion).IsSuccess);
        await worker.RunPassAsync(ct);
        var candidate = await db.CatalogRecognitionCandidates.AsNoTracking().SingleAsync(x => x.ManufacturerId == graph.Manufacturer.Id && x.Phrase == "race-phrase", ct);
        Assert.Equal(2, candidate.OccurrenceCount);
        Assert.Equal(2, candidate.DistinctProductCount);
        if (candidate.SuggestionId.HasValue)
        {
            var preview = (await Provenance(db, reviewer).ReadSuggestionAsync(candidate.SuggestionId.Value, ct)).Value;
            Assert.False(preview.SufficientEvidence);
            await using var approvalProvider = ApprovalProvider(db, reviewer);
            var result = await approvalProvider.GetRequiredService<ApproveCatalogAssistantDictionarySuggestionCommandHandler>().Handle(new(candidate.SuggestionId.Value, "race-phrase", "Characteristic", graph.Definition.Code, "16", graph.ProductType.Code, 100, null, preview.Revision), ct);
            Assert.Equal("training.conflict", result.Error.Code);
        }
        await using var second = fixture.CreateDbContext();
        var repeated = await Task.WhenAll(Provenance(db, reviewer).ExcludeAsync(sources[1].Id, "A", ct), Provenance(second, reviewer).ExcludeAsync(sources[1].Id, "B", ct));
        Assert.All(repeated, result => Assert.True(result.IsSuccess));
        Assert.Equal(repeated[0].Value.ExclusionReason, repeated[1].Value.ExclusionReason);
        Assert.Equal(1, Assert.Single(repeated[1].Value.Candidates.Items).OccurrenceCount);
    }

    [Fact]
    public async Task ConcurrentApprovalAndExclusionReturnConsistentActualImpact()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var (graph, reviewer) = await SeedGraphAsync(db, ct);
        var sources = Enumerable.Range(0, 4).Select(i => Feedback(graph, reviewer, "approval-race", CatalogRecognitionFeedbackType.Corrected, $"approval-{i}")).ToArray();
        db.CatalogRecognitionFeedbackEntries.AddRange(sources);
        await db.SaveChangesAsync(ct);
        await using var provider = CreateProvider();
        using var worker = Service(provider, new LearningLog());
        await worker.RunPassAsync(ct);
        var candidate = await db.CatalogRecognitionCandidates.AsNoTracking().SingleAsync(x => x.ManufacturerId == graph.Manufacturer.Id && x.Phrase == "approval-race", ct);
        var preview = (await Provenance(db, reviewer).ReadSuggestionAsync(candidate.SuggestionId!.Value, ct)).Value;
        await using var approvalDb = fixture.CreateDbContext();
        await using var exclusionDb = fixture.CreateDbContext();
        await AddControlAsync(approvalDb, graph, reviewer, "approval-race", ct);
        await using var approvalProvider = ApprovalProvider(approvalDb, reviewer);
        var command = new ApproveCatalogAssistantDictionarySuggestionCommand(candidate.SuggestionId.Value, "approval-race", "Characteristic", graph.Definition.Code, "16", graph.ProductType.Code, 100, null, preview.Revision);
        var report = await approvalProvider.GetRequiredService<IDictionaryEvaluationReports>().CreateAsync(command, ct);
        Assert.True(report.IsSuccess, report.IsFailure ? report.Error.Message : null);
        var approval = approvalProvider.GetRequiredService<ApproveCatalogAssistantDictionarySuggestionCommandHandler>().Handle(command with { EvaluationReportId = report.Value, Confirmed = true }, ct);
        var exclusion = Provenance(exclusionDb, reviewer).ExcludeAsync(sources[0].Id, "Race", ct);
        await Task.WhenAll(approval, exclusion);
        var approved = await approval;
        var excluded = await exclusion;
        Assert.True(excluded.IsSuccess);
        var impact = Assert.Single(excluded.Value.Candidates.Items);
        Assert.Equal(3, impact.OccurrenceCount);
        if (approved.IsSuccess) Assert.NotNull(impact.DictionaryTermId);
        else
        {
            Assert.Equal("training.conflict", approved.Error.Code);
            Assert.Null(impact.DictionaryTermId);
        }
    }

    private sealed record CurrentUser(Guid? UserId) : ElectronicService.Core.Abstractions.ICurrentUserProvider;
    private static LearningProvenanceService Provenance(ElectronicDbContext db, Guid id) =>
        new(db, new CurrentUser(id), new ElectronicService.Infrastructure.Postgres.Users.UserRepository(db),
            new ElectronicService.Infrastructure.Postgres.Users.UserPermissionOverrideRepository(db), new RecognitionMutationGate(db));
    private static ServiceProvider ApprovalProvider(ElectronicDbContext db, Guid id)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCore();
        services.AddInfrastructurePostgres(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            { ["ConnectionStrings:Database"] = db.Database.GetConnectionString() }).Build());
        services.AddSingleton(db);
        services.AddSingleton<ElectronicService.Core.Abstractions.ICurrentUserProvider>(new CurrentUser(id));
        return services.BuildServiceProvider();
    }

    private static async Task AddControlAsync(ElectronicDbContext db, CatalogProductGraph graph, Guid user, string phrase, CancellationToken ct)
    {
        var name = phrase + " 16 control";
        var feedback = CatalogRecognitionFeedback.Create(name, name.ToUpperInvariant(), graph.Manufacturer.Id, graph.ProductType.Id, graph.ProductType.Code,
            graph.Definition.Id, graph.Definition.Code, null, null, null, null, null, null, CatalogRecognitionFeedbackType.AddedManually, "16", null, null, null, null, null).Value;
        Assert.True(feedback.SetConfirmedSpan(phrase.Length + 1, 2).IsSuccess);
        Assert.True(feedback.Finalize(CatalogRecognitionLabelQuality.Strong, user, "Technical", true).IsSuccess);
        db.CatalogRecognitionFeedbackEntries.Add(feedback);
        db.CatalogRecognitionTrainingExamples.Add(CatalogRecognitionTrainingExample.Create(feedback, user).Value);
        await db.SaveChangesAsync(ct);
    }

    private ServiceProvider CreateProvider(SaveChangesInterceptor? interceptor = null, ICatalogRecognitionCandidateAggregator? aggregator = null)
    {
        using var db = fixture.CreateDbContext();
        var connection = db.Database.GetConnectionString();
        var services = new ServiceCollection();
        services.AddDbContext<ElectronicDbContext>(builder =>
        {
            builder.UseNpgsql(connection);
            if (interceptor is not null)
            {
                builder.AddInterceptors(interceptor);
            }
        });
        services.AddScoped<ICatalogRecognitionCandidateRepository, CatalogRecognitionCandidateRepository>();
        services.AddScoped<ICatalogAssistantDictionarySuggestionRepository, CatalogAssistantDictionarySuggestionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        if (aggregator is null)
        {
            services.AddScoped<ICatalogRecognitionCandidateAggregator, CatalogRecognitionCandidateAggregator>();
        }
        else
        {
            services.AddSingleton(aggregator);
        }

        services.AddScoped<IRecognitionMutationGate, ElectronicService.Infrastructure.Postgres.Catalog.Recognition.Learning.RecognitionMutationGate>();
        services.AddScoped<ICatalogRecognitionCandidateSuggestionPromoter, CatalogRecognitionCandidateSuggestionPromoter>();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static CatalogRecognitionLearningHostedService Service(ServiceProvider provider, LearningLog log)
    {
        return new CatalogRecognitionLearningHostedService(provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new CatalogRecognitionLearningOptions { PollInterval = TimeSpan.FromHours(1) }), log);
    }

    private sealed class BlockingAggregator : ICatalogRecognitionCandidateAggregator
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int CallCount { get; private set; }

        public async Task<Result<CatalogRecognitionCandidateAggregationResult, DomainError>> AggregateAsync(
            CatalogRecognitionFeedbackType feedbackType, DateTime cutoffUtc, CatalogRecognitionFeedbackCursor? after = null,
            int batchSize = 500, CancellationToken cancellationToken = default)
        {
            CallCount++;
            Entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Unreachable");
        }
    }

    private sealed class FailEvidenceSaveOnce : SaveChangesInterceptor
    {
        public Guid? FailedContext { get; private set; }
        public Guid? SuccessfulContext { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<CatalogRecognitionCandidateEvidence>().Any(x => x.State == EntityState.Added))
            {
                if (FailedContext is null)
                {
                    FailedContext = eventData.Context.ContextId.InstanceId;
                    throw new DbUpdateException("Injected evidence save failure");
                }

                SuccessfulContext = eventData.Context.ContextId.InstanceId;
            }

            return ValueTask.FromResult(result);
        }
    }

    private sealed class LearningLog : ILogger<CatalogRecognitionLearningHostedService>
    {
        public List<string> Messages { get; } = [];
        public List<Exception> Errors { get; } = [];
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            if (exception is not null)
            {
                Errors.Add(exception);
            }

            if (string.Equals(eventId.Name, "LogCompleted", StringComparison.Ordinal))
            {
                Completed.TrySetResult();
            }
        }
    }
}

