using System.Net;
using System.Net.Http.Json;
using ElectronicService.Core.Catalog.Recognition.Evaluation;
using ElectronicService.Domain.Catalog.Dictionaries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ElectronicService.Core.Catalog.Recognition.Effective;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog;

public sealed partial class RecognitionEvaluationReleaseTests
{
    [Fact]
    public async Task DictionaryEvaluationTimeoutDoesNotPersistPartialReport()
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedDictionaryAsync(ct);
        await using var app = await StartAsync(ct, "RecognitionEvaluation:TimeoutSeconds", "1",
            services => services.AddScoped<ICatalogEffectiveRecognitionService, DelayedRecognition>());
        using var client = Client(app, data.Base.UserId);
        var response = await client.PostAsJsonAsync($"{DictionaryRoot}/{data.Command.SuggestionId}/evaluations", data.Command, ct);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("evaluation.timeout", await response.Content.ReadAsStringAsync(ct), StringComparison.Ordinal);
        await using var db = fixture.CreateDbContext();
        Assert.False(await db.Set<CatalogDictionaryEvaluationReport>().AnyAsync(x => x.SuggestionId == data.Command.SuggestionId, ct));
    }

    private sealed class DelayedRecognition : ICatalogEffectiveRecognitionService
    {
        public Task<CatalogRecognitionRunContext> CreateRunAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public async Task<Result<CatalogEffectiveRecognitionResult, DomainError>> RecognizeAsync(CatalogEffectiveRecognitionRequest request, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Unreachable after cancellation");
        }
    }

    [Fact]
    public async Task TwoDifferentDictionaryReleasesCannotUseSameBaselineSnapshot()
    {
        var ct = TestContext.Current.CancellationToken;
        var first = await SeedDictionaryAsync(ct);
        var second = await SeedDictionaryAsync(ct, first.Base, "control");
        await using var app = await StartAsync(ct);
        using var client = Client(app, first.Base.UserId);
        var firstId = await CreateDictionaryReportAsync(client, first.Command, ct);
        var secondId = await CreateDictionaryReportAsync(client, second.Command, ct);
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync($"{DictionaryRoot}/{first.Command.SuggestionId}/approve", first.Command with { EvaluationReportId = firstId, Confirmed = true }, ct),
            client.PostAsJsonAsync($"{DictionaryRoot}/{second.Command.SuggestionId}/approve", second.Command with { EvaluationReportId = secondId, Confirmed = true }, ct));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.CatalogDictionaryTerms.CountAsync(x => x.ManufacturerId == first.Base.Graph.Manufacturer.Id, ct));
    }

    [Fact]
    public async Task ConcurrentDictionaryApprovalAndRejectionPreserveOneDecision()
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedDictionaryAsync(ct);
        await using var app = await StartAsync(ct);
        using var client = Client(app, data.Base.UserId);
        var id = await CreateDictionaryReportAsync(client, data.Command, ct);
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync($"{DictionaryRoot}/{data.Command.SuggestionId}/approve", data.Command with { EvaluationReportId = id, Confirmed = true }, ct),
            client.PostAsJsonAsync($"{DictionaryRoot}/{data.Command.SuggestionId}/reject", new { reviewComment = "Rejected concurrently" }, ct));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.NoContent);
        await using var db = fixture.CreateDbContext();
        var suggestion = await db.CatalogAssistantDictionarySuggestions.SingleAsync(x => x.Id == data.Command.SuggestionId, ct);
        Assert.Equal(suggestion.IsApproved ? 1 : 0, await db.CatalogDictionaryTerms.CountAsync(x => x.ManufacturerId == data.Base.Graph.Manufacturer.Id, ct));
        Assert.Equal(suggestion.IsApproved, suggestion.EvaluationReportId.HasValue);
    }

    [Fact]
    public async Task DictionaryInputLimitDoesNotSavePartialReport()
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedDictionaryAsync(ct);
        await using var app = await StartAsync(ct, "RecognitionEvaluation:MaxExamples", "1");
        using var client = Client(app, data.Base.UserId);
        var response = await client.PostAsJsonAsync($"{DictionaryRoot}/{data.Command.SuggestionId}/evaluations", data.Command, ct);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        await using var db = fixture.CreateDbContext();
        Assert.False(await db.Set<CatalogDictionaryEvaluationReport>().AnyAsync(x => x.SuggestionId == data.Command.SuggestionId, ct));
    }

    [Theory]
    [InlineData("no-control")]
    [InlineData("no-improvement")]
    [InlineData("regression")]
    public async Task DictionaryQualityFailureBlocksDirectApproval(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedDictionaryAsync(ct);
        await using var app = await StartAsync(ct);
        using var client = Client(app, data.Base.UserId);
        await using var db = fixture.CreateDbContext();
        if (string.Equals(scenario, "no-control", StringComparison.Ordinal))
        {
            var examples = await db.CatalogRecognitionTrainingExamples.Where(x => x.ManufacturerId == data.Base.Graph.Manufacturer.Id).ToArrayAsync(ct);
            foreach (var example in examples) Assert.True(example.Revoke(data.Base.UserId, "No control").IsSuccess);
        }
        else
        {
            foreach (var phrase in new[] { "16 training", "16 control" })
                db.CatalogDictionaryTerms.Add(CatalogDictionaryTerm.Create(phrase, CatalogDictionaryTermKind.Characteristic, data.Command.TargetCode, "16", 100,
                    CatalogDictionaryTermStatus.Approved, CatalogDictionaryTermSource.UserCorrection, data.Base.Graph.Manufacturer.Id, data.Base.Graph.ProductType.Id).Value);
        }
        await db.SaveChangesAsync(ct);
        var command = string.Equals(scenario, "regression", StringComparison.Ordinal) ? data.Command with { TargetValue = "25", Priority = 101 } : data.Command;
        var id = await CreateDictionaryReportAsync(client, command, ct);
        var report = (await client.GetFromJsonAsync<DictionaryEvaluationPage>($"{DictionaryRoot}/evaluations/{id}", ct))!;
        Assert.NotEqual("Ready", report.Evaluation.Readiness.State);
        if (string.Equals(scenario, "regression", StringComparison.Ordinal)) Assert.True(report.Evaluation.Control!.Regressions > 0);
        var response = await client.PostAsJsonAsync($"{DictionaryRoot}/{command.SuggestionId}/approve", command with { EvaluationReportId = id, Confirmed = true }, ct);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("evaluation.not_ready", await response.Content.ReadAsStringAsync(ct), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("foreign")]
    [InlineData("blocked")]
    [InlineData("override")]
    public async Task DictionaryReportAndApprovalEnforceOwnerAndEffectivePermission(string condition)
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedDictionaryAsync(ct);
        await using var app = await StartAsync(ct);
        using var owner = Client(app, data.Base.UserId);
        var id = await CreateDictionaryReportAsync(owner, data.Command, ct);
        await using var db = fixture.CreateDbContext();
        var user = await db.Users.SingleAsync(x => x.Id == data.Base.UserId, ct);
        var expected = HttpStatusCode.Forbidden;
        switch (condition)
        {
            case "foreign":
                user = ElectronicService.TestCommon.TestDataFactory.CreateTechnicalUser(email: $"dictionary-foreign-{Guid.NewGuid():N}@example.com");
                db.Users.Add(user); expected = HttpStatusCode.NotFound; break;
            case "blocked": Assert.True(user.Block().IsSuccess); break;
            case "override": db.UserPermissionOverrides.Add(ElectronicService.Domain.Users.UserPermissionOverride.Create(user.Id, ElectronicService.Domain.Users.Enums.UserPermissionCode.DictionariesManage, false)); break;
        }
        await db.SaveChangesAsync(ct);
        using var client = Client(app, user.Id);
        Assert.Equal(expected, (await client.GetAsync(new Uri($"{DictionaryRoot}/evaluations/{id}", UriKind.Relative), ct)).StatusCode);
        Assert.Equal(expected, (await client.PostAsync(new Uri($"{DictionaryRoot}/evaluations/{id}/replay", UriKind.Relative), null, ct)).StatusCode);
        Assert.Equal(expected, (await client.PostAsJsonAsync($"{DictionaryRoot}/{data.Command.SuggestionId}/approve", data.Command with { EvaluationReportId = id, Confirmed = true }, ct)).StatusCode);
        var create = await client.PostAsJsonAsync($"{DictionaryRoot}/{data.Command.SuggestionId}/evaluations", data.Command, ct);
        if (expected == HttpStatusCode.Forbidden) Assert.Equal(expected, create.StatusCode);
        else
        {
            Assert.Equal(HttpStatusCode.OK, create.StatusCode);
            var ownId = (await create.Content.ReadFromJsonAsync<DictionaryReportCreated>(ct))!.ReportId;
            var own = (await client.GetFromJsonAsync<DictionaryEvaluationPage>($"{DictionaryRoot}/evaluations/{ownId}", ct))!;
            Assert.Empty(own.Evaluation.Items);
            Assert.Equal("InsufficientData", own.Evaluation.Readiness.State);
        }
    }
}
