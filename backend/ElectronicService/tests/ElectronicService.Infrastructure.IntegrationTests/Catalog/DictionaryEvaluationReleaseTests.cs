using System.Net;
using System.Net.Http.Json;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;
using ElectronicService.Core.Catalog.Recognition.Evaluation;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.Recognition;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog;

public sealed partial class RecognitionEvaluationReleaseTests
{
    private const string DictionaryRoot = "/api/catalog/assistant/dictionary-suggestions";

    [Fact]
    public async Task DictionaryReleaseUsesComparisonAndPreservesHistoricalReplayAfterCleanup()
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedDictionaryAsync(ct);
        await using var app = await StartAsync(ct);
        using var client = Client(app, data.Base.UserId);
        var ruleReport = await CreateReportAsync(client, data.Base, ct);
        var reportId = await CreateDictionaryReportAsync(client, data.Command, ct);
        var before = await client.GetFromJsonAsync<DictionaryEvaluationPage>($"{DictionaryRoot}/evaluations/{reportId}", ct);
        Assert.Equal("Ready", before!.Evaluation.Readiness.State);
        Assert.Equal(3, before.Evaluation.OverlapUnits);
        Assert.Equal(2, before.Evaluation.Control!.Improvements);
        await using var db = fixture.CreateDbContext();
        Assert.False(await db.CatalogDictionaryTerms.AnyAsync(x => x.ManufacturerId == data.Base.Graph.Manufacturer.Id, ct));
        var approved = await client.PostAsJsonAsync($"{DictionaryRoot}/{data.Command.SuggestionId}/approve", data.Command with { EvaluationReportId = reportId, Confirmed = true }, ct);
        Assert.True(approved.StatusCode == HttpStatusCode.NoContent, await approved.Content.ReadAsStringAsync(ct));
        var term = await db.CatalogDictionaryTerms.SingleAsync(x => x.ManufacturerId == data.Base.Graph.Manufacturer.Id, ct);
        Assert.Equal(data.Command.Phrase, term.Phrase);
        Assert.Equal(data.Command.TargetValue, term.TargetValue);
        Assert.Equal(data.Command.Priority, term.Priority);
        var suggestion = await db.CatalogAssistantDictionarySuggestions.SingleAsync(x => x.Id == data.Command.SuggestionId, ct);
        Assert.Equal(reportId, suggestion.EvaluationReportId);
        var staleRule = await client.PostAsJsonAsync($"{Root}/rule-set-switches", Switch(data.Base, ruleReport.Id), ct);
        Assert.Equal(HttpStatusCode.Conflict, staleRule.StatusCode);
        Assert.Contains("evaluation.stale", await staleRule.Content.ReadAsStringAsync(ct), StringComparison.Ordinal);
        var preview = await client.PostAsJsonAsync("/api/catalog/recognition/preview", new { productName = "16 control", manufacturerId = data.Base.Graph.Manufacturer.Id, productTypeCode = data.Base.Graph.ProductType.Code }, ct);
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        Assert.Contains(term.Id.ToString(), await preview.Content.ReadAsStringAsync(ct), StringComparison.OrdinalIgnoreCase);
        var excluded = await client.PostAsJsonAsync($"/api/catalog/recognition/learning-provenance/feedback/{data.SourceId}/exclude", new { reason = "Historical" }, ct);
        Assert.Equal(HttpStatusCode.OK, excluded.StatusCode);
        await db.CatalogImportBatches.Where(x => x.Id == data.Base.BatchId).ExecuteDeleteAsync(ct);
        var replay = await client.PostAsync(new Uri($"{DictionaryRoot}/evaluations/{reportId}/replay", UriKind.Relative), null, ct);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var historical = (await replay.Content.ReadFromJsonAsync<DictionaryEvaluationPage>(ct))!;
        Assert.Equal(EvaluationJson.Serialize(before.Evaluation.Control), EvaluationJson.Serialize(historical.Evaluation.Control));
        Assert.Equal("Historical", historical.Evaluation.Readiness.State);
        Assert.True(historical.UsedForApproval);
    }

    [Theory]
    [InlineData("no-report")]
    [InlineData("not-confirmed")]
    [InlineData("phrase")]
    [InlineData("value")]
    [InlineData("priority")]
    [InlineData("scope")]
    [InlineData("kind")]
    [InlineData("revision")]
    public async Task DictionaryApprovalRejectsUnverifiedOrChangedDecision(string change)
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedDictionaryAsync(ct);
        await using var app = await StartAsync(ct);
        using var client = Client(app, data.Base.UserId);
        var id = await CreateDictionaryReportAsync(client, data.Command, ct);
        var command = data.Command with { EvaluationReportId = id, Confirmed = true };
        command = change switch
        {
            "no-report" => command with { EvaluationReportId = null }, "not-confirmed" => command with { Confirmed = false },
            "phrase" => command with { Phrase = "other" }, "value" => command with { TargetValue = "25" },
            "priority" => command with { Priority = 101 }, "scope" => command with { ProductTypeCode = null },
            "kind" => command with { Kind = "Manufacturer", TargetCode = null }, "revision" => command with { EvidenceRevision = 999 }, _ => command
        };
        var response = await client.PostAsJsonAsync($"{DictionaryRoot}/{command.SuggestionId}/approve", command, ct);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var db = fixture.CreateDbContext();
        Assert.False(await db.CatalogDictionaryTerms.AnyAsync(x => x.ManufacturerId == data.Base.Graph.Manufacturer.Id, ct));
        Assert.True((await db.CatalogAssistantDictionarySuggestions.SingleAsync(x => x.Id == command.SuggestionId, ct)).IsPending);
    }

    [Theory]
    [InlineData("revoke")]
    [InlineData("exclude")]
    [InlineData("baseline")]
    [InlineData("schema")]
    [InlineData("new-confirmation")]
    public async Task DictionaryApprovalRejectsChangedDependencies(string change)
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedDictionaryAsync(ct);
        await using var app = await StartAsync(ct);
        using var client = Client(app, data.Base.UserId);
        var id = await CreateDictionaryReportAsync(client, data.Command, ct);
        await using var db = fixture.CreateDbContext();
        switch (change)
        {
            case "revoke":
                Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/catalog/recognition/training-examples/{data.Base.ControlId}/revoke", new { reason = "Changed" }, ct)).StatusCode);
                break;
            case "exclude":
                Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/catalog/recognition/learning-provenance/feedback/{data.Base.ControlFeedbackId}/exclude", new { reason = "Changed" }, ct)).StatusCode);
                break;
            case "baseline":
                db.CatalogDictionaryTerms.Add(CatalogDictionaryTerm.Create("other", CatalogDictionaryTermKind.Characteristic, data.Command.TargetCode, "25", 100,
                    CatalogDictionaryTermStatus.Approved, CatalogDictionaryTermSource.UserCorrection, data.Base.Graph.Manufacturer.Id, data.Base.Graph.ProductType.Id).Value);
                await db.SaveChangesAsync(ct);
                break;
            case "schema":
                await db.CharacteristicDefinitions.Where(x => x.Id == data.Base.Graph.Definition.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Name, "Changed"), ct);
                break;
            case "new-confirmation":
                var (feedback, example) = Example(data.Base.Graph, data.Base.UserId, "16 additional");
                db.CatalogRecognitionFeedbackEntries.Add(feedback);
                db.CatalogRecognitionTrainingExamples.Add(example);
                await db.SaveChangesAsync(ct);
                break;
        }
        var response = await client.PostAsJsonAsync($"{DictionaryRoot}/{data.Command.SuggestionId}/approve", data.Command with { EvaluationReportId = id, Confirmed = true }, ct);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("evaluation.stale", await response.Content.ReadAsStringAsync(ct), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConcurrentDictionaryApprovalsCreateOneTerm()
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedDictionaryAsync(ct);
        await using var app = await StartAsync(ct);
        using var client = Client(app, data.Base.UserId);
        var id = await CreateDictionaryReportAsync(client, data.Command, ct);
        var command = data.Command with { EvaluationReportId = id, Confirmed = true };
        var responses = await Task.WhenAll(client.PostAsJsonAsync($"{DictionaryRoot}/{command.SuggestionId}/approve", command, ct),
            client.PostAsJsonAsync($"{DictionaryRoot}/{command.SuggestionId}/approve", command, ct));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(responses, x => !x.IsSuccessStatusCode);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.CatalogDictionaryTerms.CountAsync(x => x.ManufacturerId == data.Base.Graph.Manufacturer.Id, ct));
    }

    private static async Task<Guid> CreateDictionaryReportAsync(HttpClient client, ApproveCatalogAssistantDictionarySuggestionCommand command, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync($"{DictionaryRoot}/{command.SuggestionId}/evaluations", command, ct);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync(ct));
        return (await response.Content.ReadFromJsonAsync<DictionaryReportCreated>(ct))!.ReportId;
    }
    private sealed record DictionaryReportCreated(Guid ReportId);
    private sealed record DictionarySeed(Seed Base, ApproveCatalogAssistantDictionarySuggestionCommand Command, Guid SourceId);
    private async Task<DictionarySeed> SeedDictionaryAsync(CancellationToken ct, Seed? existing = null, string phrase = "16")
    {
        var data = existing ?? await SeedAsync(ct);
        await using var db = fixture.CreateDbContext();
        var graph = data.Graph;
        var candidate = CatalogRecognitionCandidate.Create(phrase, phrase.ToUpperInvariant(), graph.Manufacturer.Id, graph.ProductType.Id, graph.ProductType.Code,
            graph.Definition.Id, graph.Definition.Code, "16", CatalogRecognitionFeedbackType.Corrected, DateTime.UtcNow).Value;
        candidate.RecalculateEvidence(0, 3, 0, 3);
        var suggestion = CatalogAssistantDictionarySuggestion.CreateFromRecognitionCandidate(candidate, 1m, data.UserId).Value;
        Assert.True(candidate.AttachSuggestion(suggestion.Id).IsSuccess);
        db.CatalogRecognitionCandidates.Add(candidate);
        db.CatalogAssistantDictionarySuggestions.Add(suggestion);
        var first = Guid.Empty;
        for (var i = 0; i < 3; i++)
        {
            var name = $"{phrase} learned {i}";
            var feedback = CatalogRecognitionFeedback.Create(name, name.ToUpperInvariant(), graph.Manufacturer.Id, graph.ProductType.Id, graph.ProductType.Code,
                graph.Definition.Id, graph.Definition.Code, "10", "10", 0.9m, "test", 0, 2, CatalogRecognitionFeedbackType.Corrected, "16", null, null, null, null, null).Value;
            Assert.True(feedback.SetConfirmedSpan(0, 2).IsSuccess);
            Assert.True(feedback.Finalize(CatalogRecognitionLabelQuality.Strong, data.UserId, "Technical", true).IsSuccess);
            db.CatalogRecognitionFeedbackEntries.Add(feedback);
            db.CatalogRecognitionCandidateEvidenceEntries.Add(CatalogRecognitionCandidateEvidence.Create(candidate.Id, feedback.Id).Value);
            var example = CatalogRecognitionTrainingExample.Create(feedback, data.UserId).Value;
            db.CatalogRecognitionTrainingExamples.Add(example);
            db.Entry(example).Property(x => x.IsEvaluationOnly).CurrentValue = true;
            if (i == 0) first = feedback.Id;
        }
        await db.SaveChangesAsync(ct);
        return new(data, new(suggestion.Id, phrase, "Characteristic", graph.Definition.Code, "16", graph.ProductType.Code, 100, "Checked", candidate.EvidenceRevision), first);
    }
}
