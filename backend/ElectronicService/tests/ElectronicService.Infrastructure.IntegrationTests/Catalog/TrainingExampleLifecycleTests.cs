using System.Text;
using System.Text.Json;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Infrastructure.IntegrationTests.Data;
using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using ElectronicService.Infrastructure.Postgres.Catalog.ImportBatches.Cleanup;
using ElectronicService.Infrastructure.Postgres.Catalog.Queries;
using ElectronicService.Infrastructure.Postgres.Catalog.Repositories;
using ElectronicService.Infrastructure.Postgres.Data;
using ElectronicService.Infrastructure.Postgres.Users;
using ElectronicService.TestCommon;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed class TrainingExampleLifecycleTests(PostgreSqlFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RevokedEvidenceInvalidatesAllDraftKindsWithoutDeletingHistory(bool excludeSource)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var graph = PostgreSqlTestDataFactory.CreateCatalogProductGraph();
        var owner = TestDataFactory.CreateTechnicalUser(email: $"drafts-{Guid.NewGuid():N}@example.com");
        var second = TestDataFactory.CreateCharacteristicDefinition($"SECOND_{Guid.NewGuid():N}", "Second", CharacteristicDataType.Number, null);
        TestDataFactory.AddCharacteristic(graph.ProductType, second);
        db.Users.Add(owner);
        db.Manufacturers.Add(graph.Manufacturer);
        db.ProductTypes.Add(graph.ProductType);
        db.CharacteristicDefinitions.AddRange(graph.Definition, second);
        var examples = new List<CatalogRecognitionTrainingExample>();
        foreach (var (name, firstValue, secondValue) in new[] { ("X16A 20B", "16", "20"), ("X32A 40B", "32", "40") })
        {
            foreach (var (definition, value, start) in new[] { (graph.Definition, firstValue, 1), (second, secondValue, 5) })
            {
                var feedback = CatalogRecognitionFeedback.Create(name, name, graph.Manufacturer.Id,
                    graph.ProductType.Id, graph.ProductType.Code, definition.Id, definition.Code,
                    null, null, null, null, null, null, CatalogRecognitionFeedbackType.AddedManually,
                    value, null, null, null, null, null).Value;
                Assert.True(feedback.SetConfirmedSpan(start, 2).IsSuccess);
                Assert.True(feedback.Finalize(CatalogRecognitionLabelQuality.Strong, owner.Id, "Technical", true).IsSuccess);
                db.CatalogRecognitionFeedbackEntries.Add(feedback);
                var example = CatalogRecognitionTrainingExample.Create(feedback, owner.Id).Value;
                examples.Add(example);
                db.CatalogRecognitionTrainingExamples.Add(example);
            }
        }
        var firstIds = examples.Where(x => x.CharacteristicDefinitionId == graph.Definition.Id).Select(x => x.Id).ToArray();
        var literal = CatalogRecognitionLiteralDraft.Create(graph.Manufacturer.Id, graph.ProductType.Id,
            graph.Definition.Id, "16", "16", CatalogRecognitionLiteralProposalGenerator.Version, 1, owner.Id,
            firstIds, [firstIds[0]]).Value;
        var integer = CatalogRecognitionIntegerDraft.Create(graph.Manufacturer.Id, graph.ProductType.Id,
            graph.Definition.Id, "X", ["A"], CatalogRecognitionIntegerAlternativesGenerator.Version, 2, 2,
            owner.Id, firstIds, firstIds).Value;
        var allIds = examples.Select(x => x.Id).ToArray();
        var multi = CatalogRecognitionMultiIntegerDraft.Create(new(graph.Manufacturer.Id, graph.ProductType.Id,
            CatalogRecognitionMultiIntegerProposalGenerator.Version,
            [new("X", null, 0), new(null, graph.Definition.Id, 2), new("A", null, 0), new(" ", null, 0), new(null, second.Id, 2), new("B", null, 0)],
            2, 2, owner.Id, allIds, allIds)).Value;
        db.CatalogRecognitionLiteralDrafts.Add(literal);
        db.CatalogRecognitionIntegerDrafts.Add(integer);
        db.CatalogRecognitionMultiIntegerDrafts.Add(multi);
        var version = CatalogRecognitionRuleSetVersion.Create(new(graph.Manufacturer.Id, graph.ProductType.Id, 1, "Linked version", owner.Id,
            [new(CatalogRecognitionRuleKind.Literal, literal.Id), new(CatalogRecognitionRuleKind.NumericCapture, integer.Id), new(CatalogRecognitionRuleKind.MultipleNumericCaptures, multi.Id)])).Value;
        db.CatalogRecognitionRuleSetVersions.Add(version);
        await db.SaveChangesAsync(ct);
        var reportBatch = CatalogImportBatch.Create(owner.Id, "report.xlsx", "application/octet-stream", [1]).Value;
        db.CatalogImportBatches.Add(reportBatch);
        var now = DateTime.UtcNow;
        var report = CatalogRecognitionRuleSetReport.Create(new(version.Id, reportBatch.Id, 0, owner.Id, now, now, "test", 1, 1, 0, 0, 0, "[{}]")).Value;
        db.CatalogRecognitionRuleSetReports.Add(report);
        db.CatalogRecognitionRuleSetSwitches.Add(CatalogRecognitionRuleSetSwitch.Create(new(graph.Manufacturer.Id, graph.ProductType.Id, 1, null, version.Id, report.Id, owner.Id, "Initial")).Value);
        await db.SaveChangesAsync(ct);
        var lineage = await Provenance(db, owner.Id).ReadAsync(firstIds[0], true, 1, ct);
        Assert.True(lineage.IsSuccess);
        Assert.Equal(3, lineage.Value.Drafts.Total);
        Assert.Equal(3, lineage.Value.Versions.Total);
        Assert.All(lineage.Value.Versions.Items, v => { Assert.Equal(version.Id, v.Id); Assert.True(v.Active); });
        var samples = new CatalogRecognitionTrainingSampleReader(db);
        var metadata = new CatalogProductMetadataRepository(db);
        var definitions = new CharacteristicDefinitionRepository(db);
        var literalCheck = new CatalogRecognitionLiteralDraftRecheckService(new CatalogRecognitionLiteralDraftReader(db), samples);
        var integerCheck = new CatalogRecognitionIntegerDraftRecheckService(new CatalogRecognitionIntegerDraftReader(db), samples, metadata, definitions);
        var multiCheck = new CatalogRecognitionMultiIntegerDraftRecheckService(new CatalogRecognitionMultiIntegerDraftReader(db),
            new CatalogRecognitionMultiIntegerProposalService(samples, metadata, definitions));
        Assert.True((await literalCheck.RecheckAsync(literal.Id, ct))!.EvidenceUnchanged);
        Assert.True((await integerCheck.RecheckAsync(integer.Id, ct))!.EvidenceUnchanged);
        var multiBefore = await multiCheck.RecheckAsync(multi.Id, ct);
        Assert.True(multiBefore!.EvidenceUnchanged, JsonSerializer.Serialize(multiBefore));
        if (excludeSource)
        {
            var excluded = await Provenance(db, owner.Id).ExcludeAsync(examples[0].SourceFeedbackId, "Ошибка источника", ct);
            Assert.True(excluded.IsSuccess);
            Assert.All(excluded.Value.Versions.Items, v => { Assert.True(v.NeedsReview); Assert.True(v.Active); });
            var gate = new ElectronicService.Infrastructure.Postgres.Catalog.Recognition.Learning.RecognitionMutationGate(db);
            Assert.Equal("training.conflict", (await new CatalogRecognitionLiteralDraftRepository(db, gate).SaveAsync(literal, ct)).Error.Code);
            Assert.Equal("training.conflict", (await new CatalogRecognitionIntegerDraftRepository(db, gate).SaveAsync(integer, ct)).Error.Code);
            Assert.Equal("training.conflict", (await new CatalogRecognitionMultiIntegerDraftRepository(db, gate).SaveAsync(multi, ["X16A 20B", "X32A 40B"], ct)).Error.Code);
        }
        else Assert.True((await Service(db, owner.Id).RevokeAsync(firstIds[0], "Исправление основания", ct)).IsSuccess);
        Assert.False((await literalCheck.RecheckAsync(literal.Id, ct))!.EvidenceUnchanged);
        Assert.False((await integerCheck.RecheckAsync(integer.Id, ct))!.EvidenceUnchanged);
        Assert.False((await multiCheck.RecheckAsync(multi.Id, ct))!.Passed);
        Assert.True(await db.CatalogRecognitionLiteralDrafts.AnyAsync(x => x.Id == literal.Id, ct));
        Assert.True(await db.CatalogRecognitionIntegerDrafts.AnyAsync(x => x.Id == integer.Id, ct));
        Assert.True(await db.CatalogRecognitionMultiIntegerDrafts.AnyAsync(x => x.Id == multi.Id, ct));
        db.CatalogRecognitionRuleSetSwitches.Add(CatalogRecognitionRuleSetSwitch.Create(new(graph.Manufacturer.Id, graph.ProductType.Id, 2, version.Id, null, null, owner.Id, "Disable")).Value);
        await db.SaveChangesAsync(ct);
        var switchedOff = await Provenance(db, owner.Id).ReadAsync(firstIds[0], true, 1, ct);
        Assert.All(switchedOff.Value.Versions.Items, v => Assert.False(v.Active));
        Assert.Equal(2, switchedOff.Value.Switches.Total);
    }

    [Fact]
    public async Task CleanupDoesNotPreventRevocationAndExportsHaveDistinctSemantics()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var (owner, example, batchId) = await SeedAsync(db, ct);
        var service = Service(db, owner);
        var filter = new TrainingExampleFilter(ManufacturerId: example.ManufacturerId);
        var before = await service.ListAsync(filter, ct);
        Assert.True(before.IsSuccess);
        Assert.Single(before.Value.Items);
        await using var output = new MemoryStream();
        var export = await service.ExportAsync(filter, output, ct);
        Assert.True(export.IsSuccess);
        Assert.Equal(1, export.Value.Count);
        var lines = Encoding.UTF8.GetString(output.ToArray()).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        using var metadata = JsonDocument.Parse(lines[0]);
        Assert.Equal("UTF-16 code units", metadata.RootElement.GetProperty("spanUnits").GetString());
        using var record = JsonDocument.Parse(lines[1]);
        Assert.Equal("16", record.RootElement.GetProperty("rawValue").GetString());
        Assert.Equal(3, record.RootElement.GetProperty("spanStart").GetInt32());
        Assert.Equal("😀 16A", record.RootElement.GetProperty("productName").GetString());

        await db.CatalogImportBatches.Where(x => x.Id == batchId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.CreatedAtUtc, DateTime.UtcNow.AddDays(-100)), ct);
        Assert.True(await new CatalogImportCleanupService(db).DeleteExpiredAsync(DateTime.UtcNow.AddDays(-30), 100, ct) > 0);
        var afterCleanup = await service.GetAsync(example.Id, ct);
        Assert.True(afterCleanup.IsSuccess);
        Assert.Null(afterCleanup.Value.ImportBatchId);
        Assert.True((await service.RevokeAsync(example.Id, "Ошибка разметки", ct)).IsSuccess);
        var first = (await service.GetAsync(example.Id, ct)).Value;
        Assert.True((await service.RevokeAsync(example.Id, "Повтор", ct)).IsSuccess);
        var second = (await service.GetAsync(example.Id, ct)).Value;
        Assert.Equal(first.RevokedAtUtc, second.RevokedAtUtc);
        Assert.Equal("Ошибка разметки", second.RevocationReason);
        Assert.Empty((await service.ListAsync(filter, ct)).Value.Items);
        Assert.Single((await service.ListAsync(filter with { Status = "revoked" }, ct)).Value.Items);
        await using var after = new MemoryStream();
        Assert.Equal(0, (await service.ExportAsync(filter, after, ct)).Value.Count);
        var sample = await new CatalogRecognitionTrainingSampleReader(db).ReadAsync(
            new(example.ManufacturerId, example.ProductTypeId, example.CharacteristicDefinitionId), cancellationToken: ct);
        Assert.Empty(sample.Samples);
        var feedbackIds = new List<Guid>();
        await foreach (var feedback in new CatalogRecognitionDatasetReader(db).StreamTrainingEligibleAsync(DateTime.UtcNow, ct))
            feedbackIds.Add(feedback.FeedbackId);
        Assert.Contains(example.SourceFeedbackId, feedbackIds);

        var source = await db.CatalogRecognitionFeedbackEntries.SingleAsync(x => x.Id == example.SourceFeedbackId, ct);
        var replacement = CatalogRecognitionTrainingExample.Create(source, owner);
        Assert.True(replacement.IsSuccess);
        var saved = await new CatalogRecognitionTrainingExampleRepository(db).AddOrGetActiveAsync(replacement.Value, ct);
        Assert.NotEqual(example.Id, saved.Id);
        Assert.Single((await service.ListAsync(filter, ct)).Value.Items);
        Assert.Single((await service.ListAsync(filter with { Status = "revoked" }, ct)).Value.Items);
    }

    [Fact]
    public async Task OwnershipPermissionAndActiveAccountAreRequiredForEveryOperation()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var (owner, example, _) = await SeedAsync(db, ct);
        var stranger = TestDataFactory.CreateTechnicalUser(email: $"stranger-{Guid.NewGuid():N}@example.com");
        db.Users.Add(stranger);
        await db.SaveChangesAsync(ct);
        var other = Service(db, stranger.Id);
        var filter = new TrainingExampleFilter(ManufacturerId: example.ManufacturerId);
        Assert.Empty((await other.ListAsync(filter, ct)).Value.Items);
        Assert.Equal("training.not_found", (await other.GetAsync(example.Id, ct)).Error.Code);
        Assert.Equal("training.not_found", (await other.RevokeAsync(example.Id, "Чужой пример", ct)).Error.Code);
        await using var output = new MemoryStream();
        Assert.Equal(0, (await other.ExportAsync(filter, output, ct)).Value.Count);

        var user = await db.Users.SingleAsync(x => x.Id == owner, ct);
        Assert.True(user.Block().IsSuccess);
        await db.SaveChangesAsync(ct);
        await AssertForbiddenAsync(Service(db, owner), example.Id, filter, ct);
        Assert.True(user.Activate().IsSuccess);
        Assert.True(user.MakeRegular().IsSuccess);
        await db.SaveChangesAsync(ct);
        await AssertForbiddenAsync(Service(db, owner), example.Id, filter, ct);
    }

    [Fact]
    public async Task ConcurrentRevocationsPreserveOneCompleteDecision()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var setup = fixture.CreateDbContext();
        var (owner, example, _) = await SeedAsync(setup, ct);
        await using var db1 = fixture.CreateDbContext();
        await using var db2 = fixture.CreateDbContext();
        var results = await Task.WhenAll(Service(db1, owner).RevokeAsync(example.Id, "Первый", ct),
            Service(db2, owner).RevokeAsync(example.Id, "Второй", ct));
        Assert.All(results, x => Assert.True(x.IsSuccess));
        var saved = await setup.CatalogRecognitionTrainingExamples.AsNoTracking().SingleAsync(x => x.Id == example.Id, ct);
        Assert.NotNull(saved.RevokedAtUtc);
        Assert.Equal(owner, saved.RevokedByUserId);
        Assert.True(saved.RevocationReason is "Первый" or "Второй");
    }

    [Theory]
    [InlineData(CatalogRecognitionFeedbackType.Corrected)]
    [InlineData(CatalogRecognitionFeedbackType.Accepted)]
    [InlineData(CatalogRecognitionFeedbackType.Rejected)]
    public async Task ExclusionPreservesHistoryAndRecalculatesDuplicateNamesToZero(CatalogRecognitionFeedbackType otherType)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var (owner, example, batchId) = await SeedAsync(db, ct);
        var feedback = await db.CatalogRecognitionFeedbackEntries.SingleAsync(x => x.Id == example.SourceFeedbackId, ct);
        var candidate = CatalogRecognitionCandidate.Create("16", "16", example.ManufacturerId, example.ProductTypeId,
            feedback.ProductTypeCodeSnapshot!, example.CharacteristicDefinitionId, feedback.CharacteristicCodeSnapshot!, "16",
            CatalogRecognitionFeedbackType.Corrected, DateTime.UtcNow).Value;
        db.CatalogRecognitionCandidates.Add(candidate);
        db.CatalogRecognitionCandidateEvidenceEntries.Add(CatalogRecognitionCandidateEvidence.Create(candidate.Id, feedback.Id).Value);
        var other = CatalogRecognitionFeedback.Create(feedback.ProductName, feedback.NormalizedProductName, example.ManufacturerId,
            example.ProductTypeId, feedback.ProductTypeCodeSnapshot, example.CharacteristicDefinitionId, feedback.CharacteristicCodeSnapshot,
            "10", otherType == CatalogRecognitionFeedbackType.Accepted ? "16" : "10", 0.9m, "test", null, null, otherType, otherType == CatalogRecognitionFeedbackType.Rejected ? null : "16", null, null, null, null, null).Value;
        Assert.True(other.Finalize(CatalogRecognitionLabelQuality.Strong, owner, "Technical", true).IsSuccess);
        db.CatalogRecognitionFeedbackEntries.Add(other);
        db.CatalogRecognitionCandidateEvidenceEntries.Add(CatalogRecognitionCandidateEvidence.Create(candidate.Id, other.Id).Value);
        Assert.True(candidate.AddEvidence(otherType, DateTime.UtcNow, false).IsSuccess);
        await db.SaveChangesAsync(ct);
        await db.CatalogImportBatches.Where(x => x.Id == batchId).ExecuteDeleteAsync(ct);
        var provenance = Provenance(db, owner);
        var before = await provenance.ReadAsync(example.Id, true, 1, ct);
        Assert.True(before.IsSuccess);
        Assert.True(before.Value.CanExclude);
        Assert.Equal(candidate.Id, Assert.Single(before.Value.Candidates.Items).Id);
        await using (var failing = new ElectronicDbContext(new DbContextOptionsBuilder<ElectronicDbContext>()
            .UseNpgsql(db.Database.GetConnectionString()).AddInterceptors(new FailCandidateRecalculation()).Options))
        {
            await Assert.ThrowsAsync<DbUpdateException>(() => Provenance(failing, owner).ExcludeAsync(feedback.Id, "Откат", ct));
        }
        Assert.Null((await db.CatalogRecognitionFeedbackEntries.AsNoTracking().SingleAsync(x => x.Id == feedback.Id, ct)).ExcludedAtUtc);
        Assert.Equal(2, (await db.CatalogRecognitionCandidates.AsNoTracking().SingleAsync(x => x.Id == candidate.Id, ct)).OccurrenceCount);
        var excluded = await provenance.ExcludeAsync(feedback.Id, "Ошибка исходного наблюдения", ct);
        Assert.True(excluded.IsSuccess);
        var current = Assert.Single(excluded.Value.Candidates.Items);
        Assert.Equal(1, current.OccurrenceCount);
        Assert.Equal(1, current.DistinctProductCount);
        Assert.Empty((await Service(db, owner).ListAsync(new(ManufacturerId: example.ManufacturerId), ct)).Value.Items);
        var history = (await Service(db, owner).GetAsync(example.Id, ct)).Value;
        Assert.NotNull(history.SourceExcludedAtUtc);
        Assert.Null(history.RevokedAtUtc);
        await using var output = new MemoryStream();
        Assert.Equal(0, (await Service(db, owner).ExportAsync(new(ManufacturerId: example.ManufacturerId), output, ct)).Value.Count);
        Assert.False(await db.CatalogRecognitionFeedbackEntries.AnyAsync(x => x.Id == feedback.Id && x.ExcludedAtUtc == null, ct));
        var datasetIds = new List<Guid>();
        await foreach (var item in new CatalogRecognitionDatasetReader(db).StreamTrainingEligibleAsync(DateTime.UtcNow, ct)) datasetIds.Add(item.FeedbackId);
        Assert.DoesNotContain(feedback.Id, datasetIds);
        Assert.Contains(other.Id, datasetIds);
        var saved = await db.CatalogRecognitionFeedbackEntries.AsNoTracking().SingleAsync(x => x.Id == feedback.Id, ct);
        Assert.True(saved.IsTrainingEligible);
        Assert.True(CatalogRecognitionTrainingExample.Create(saved, owner).IsFailure);
        var repeat = await provenance.ExcludeAsync(feedback.Id, "Другая причина", ct);
        Assert.Equal(excluded.Value.ExclusionReason, repeat.Value.ExclusionReason);
        Assert.Equal(excluded.Value.ExcludedAtUtc, repeat.Value.ExcludedAtUtc);
        var zero = await provenance.ExcludeAsync(other.Id, "Последнее наблюдение", ct);
        Assert.Equal(0, Assert.Single(zero.Value.Candidates.Items).OccurrenceCount);
        Assert.Equal(0, Assert.Single(zero.Value.Candidates.Items).DistinctProductCount);
        Assert.Equal(2, await db.CatalogRecognitionCandidateEvidenceEntries.CountAsync(x => x.CandidateId == candidate.Id, ct));
    }

    [Fact]
    public async Task ExampleAuthorCanReadButCannotExcludeAnotherReviewersFeedback()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var (owner, example, _) = await SeedAsync(db, ct);
        var other = TestDataFactory.CreateTechnicalUser(email: $"provenance-{Guid.NewGuid():N}@example.com");
        db.Users.Add(other);
        db.Entry(example).Property(x => x.ConfirmedByUserId).CurrentValue = other.Id;
        await db.SaveChangesAsync(ct);
        var read = await Provenance(db, other.Id).ReadAsync(example.Id, true, 1, ct);
        Assert.True(read.IsSuccess);
        Assert.False(read.Value.CanExclude);
        var source = await db.CatalogRecognitionFeedbackEntries.SingleAsync(x => x.Id == example.SourceFeedbackId, ct);
        for (var i = 0; i < 30; i++)
        {
            var historical = CatalogRecognitionTrainingExample.Create(source, owner).Value;
            Assert.True(historical.Revoke(owner, "Historical").IsSuccess);
            db.CatalogRecognitionTrainingExamples.Add(historical);
        }
        await db.SaveChangesAsync(ct);
        var secondPage = (await Provenance(db, other.Id).ReadAsync(example.Id, true, 2, ct)).Value;
        Assert.Equal(31, secondPage.Examples.Total);
        Assert.Equal(6, secondPage.Examples.Items.Count);
        Assert.Equal("training.forbidden", (await Provenance(db, other.Id).ExcludeAsync(example.SourceFeedbackId, "Чужое", ct)).Error.Code);
        Assert.True((await Provenance(db, owner).ExcludeAsync(example.SourceFeedbackId, "Моё решение", ct)).IsSuccess);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingExclusionRequiresExistingBatchOwnership(bool deleteBatch)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var (owner, original, batchId) = await SeedAsync(db, ct);
        var originalSource = await db.CatalogRecognitionFeedbackEntries.SingleAsync(x => x.Id == original.SourceFeedbackId, ct);
        var pendingRow = CatalogImportRow.Create(batchId, 3, CatalogImportRowStatus.Valid, "{}", "{}", "[]", "[]").Value;
        db.CatalogImportRows.Add(pendingRow);
        var pending = CatalogRecognitionFeedback.Create("16A", "16A", original.ManufacturerId, original.ProductTypeId,
            originalSource.ProductTypeCodeSnapshot, original.CharacteristicDefinitionId, originalSource.CharacteristicCodeSnapshot,
            null, null, null, null, null, null, CatalogRecognitionFeedbackType.AddedManually, "16", null, null, null, batchId, pendingRow.Id).Value;
        Assert.True(pending.SetConfirmedSpan(0, 2).IsSuccess);
        var example = CatalogRecognitionTrainingExample.Create(pending, owner).Value;
        db.CatalogRecognitionFeedbackEntries.Add(pending);
        db.CatalogRecognitionTrainingExamples.Add(example);
        await db.SaveChangesAsync(ct);
        if (deleteBatch) await db.CatalogImportBatches.Where(x => x.Id == batchId).ExecuteDeleteAsync(ct);
        var provenance = Provenance(db, owner);
        var read = await provenance.ReadAsync(example.Id, true, 1, ct);
        Assert.True(read.IsSuccess);
        Assert.Equal(!deleteBatch, read.Value.CanExclude);
        var excluded = await provenance.ExcludeAsync(pending.Id, "Pending source", ct);
        Assert.Equal(!deleteBatch, excluded.IsSuccess);
        if (deleteBatch) Assert.Equal("training.forbidden", excluded.Error.Code);
        Assert.True((await Service(db, owner).RevokeAsync(example.Id, "Own confirmation", ct)).IsSuccess);
        var user = await db.Users.SingleAsync(x => x.Id == owner, ct);
        Assert.True(user.Block().IsSuccess);
        await db.SaveChangesAsync(ct);
        Assert.Equal("training.forbidden", (await provenance.ReadAsync(example.Id, true, 1, ct)).Error.Code);
        Assert.Equal("training.forbidden", (await provenance.ExcludeAsync(pending.Id, "Blocked", ct)).Error.Code);
    }

    private sealed class FailCandidateRecalculation : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
    {
        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> SavingChangesAsync(
            Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData eventData,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<CatalogRecognitionCandidate>().Any(x => x.State == EntityState.Modified))
                throw new DbUpdateException("Injected recalculation failure");
            return ValueTask.FromResult(result);
        }
    }

    private static LearningProvenanceService Provenance(ElectronicDbContext db, Guid id) =>
        new(db, new CurrentUser(id), new UserRepository(db), new UserPermissionOverrideRepository(db),
            new ElectronicService.Infrastructure.Postgres.Catalog.Recognition.Learning.RecognitionMutationGate(db));

    private static async Task AssertForbiddenAsync(CatalogTrainingExampleManagement service, Guid id, TrainingExampleFilter filter, CancellationToken ct)
    {
        Assert.Equal("training.forbidden", (await service.ListAsync(filter, ct)).Error.Code);
        Assert.Equal("training.forbidden", (await service.GetAsync(id, ct)).Error.Code);
        Assert.Equal("training.forbidden", (await service.RevokeAsync(id, "Причина", ct)).Error.Code);
        await using var output = new MemoryStream();
        Assert.Equal("training.forbidden", (await service.ExportAsync(filter, output, ct)).Error.Code);
    }

    private static CatalogTrainingExampleManagement Service(ElectronicDbContext db, Guid id) =>
        new(db, new CurrentUser(id), new UserRepository(db), new UserPermissionOverrideRepository(db));

    private sealed record CurrentUser(Guid? UserId) : ICurrentUserProvider;

    private static async Task<(Guid Owner, CatalogRecognitionTrainingExample Example, Guid BatchId)> SeedAsync(ElectronicDbContext db, CancellationToken ct)
    {
        var graph = PostgreSqlTestDataFactory.CreateCatalogProductGraph();
        var user = TestDataFactory.CreateTechnicalUser(email: $"lifecycle-{Guid.NewGuid():N}@example.com");
        db.Users.Add(user);
        db.Manufacturers.Add(graph.Manufacturer);
        db.CharacteristicDefinitions.Add(graph.Definition);
        db.ProductTypes.Add(graph.ProductType);
        var batch = CatalogImportBatch.Create(user.Id, "test.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", [1]).Value;
        db.CatalogImportBatches.Add(batch);
        var row = CatalogImportRow.Create(batch.Id, 2, CatalogImportRowStatus.Valid, "{}", "{}", "[]", "[]").Value;
        db.CatalogImportRows.Add(row);
        var feedback = CatalogRecognitionFeedback.Create("😀 16A", "😀 16A", graph.Manufacturer.Id,
            graph.ProductType.Id, graph.ProductType.Code, graph.Definition.Id, graph.Definition.Code,
            "10", "10", 0.9m, "test", null, null, CatalogRecognitionFeedbackType.Corrected,
            "16", null, null, null, batch.Id, row.Id).Value;
        Assert.True(feedback.SetConfirmedSpan(3, 2).IsSuccess);
        Assert.True(feedback.Finalize(CatalogRecognitionLabelQuality.Strong, user.Id, "Technical", true).IsSuccess);
        db.CatalogRecognitionFeedbackEntries.Add(feedback);
        var example = CatalogRecognitionTrainingExample.Create(feedback, user.Id).Value;
        db.CatalogRecognitionTrainingExamples.Add(example);
        await db.SaveChangesAsync(ct);
        return (user.Id, example, batch.Id);
    }
}
