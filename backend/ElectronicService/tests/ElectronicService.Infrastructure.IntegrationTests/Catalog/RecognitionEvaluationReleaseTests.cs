using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ElectronicService.Core;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.Recognition.Evaluation;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Infrastructure.IntegrationTests.Data;
using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using ElectronicService.Infrastructure.Postgres;
using ElectronicService.Infrastructure.Postgres.Catalog.ImportBatches.Cleanup;
using ElectronicService.Infrastructure.Postgres.Catalog.Recognition.Learning;
using ElectronicService.TestCommon;
using ElectronicService.Web.Auth;
using ElectronicService.Web.Controllers.Catalog.Recognition.SwitchRuleSet;
using ElectronicService.Web.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed partial class RecognitionEvaluationReleaseTests(PostgreSqlFixture fixture)
{
    private const string Root = "/api/catalog/recognition/training";

    [Fact]
    public async Task RealHttpReportReleaseReplayAndCleanupPreserveSnapshotAndPermissions()
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedAsync(ct);
        await using var app = await StartAsync(ct);
        using var client = Client(app, data.UserId);
        var report = await CreateReportAsync(client, data, ct);
        var page = await client.GetFromJsonAsync<EvaluationPage>($"{Root}/rule-set-reports/{report.Id}/evaluation", ct);
        Assert.Equal("Ready", page!.Readiness.State);
        Assert.Equal(1, page.Control!.Improvements);
        Assert.True(page.TrainingPassed);
        Assert.Equal(1, page.OverlapUnits);
        var activated = await client.PostAsJsonAsync($"{Root}/rule-set-switches", Switch(data, report.Id), ct);
        Assert.Equal(HttpStatusCode.OK, activated.StatusCode);
        var preview = await client.PostAsJsonAsync("/api/catalog/recognition/preview", new { productName = "16 control", manufacturerId = data.Graph.Manufacturer.Id, productTypeCode = data.Graph.ProductType.Code }, ct);
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        var previewJson = await preview.Content.ReadAsStringAsync(ct);
        Assert.Contains(data.VersionId.ToString(), previewJson, StringComparison.OrdinalIgnoreCase);
        await using (var db = fixture.CreateDbContext())
        {
            var term = ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTerm.Create("16", ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTermKind.Characteristic,
                data.Graph.Definition.Code, "25", 100, ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTermStatus.Approved,
                ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTermSource.UserCorrection, data.Graph.Manufacturer.Id, data.Graph.ProductType.Id).Value;
            db.CatalogDictionaryTerms.Add(term);
            var excluded = await db.CatalogRecognitionFeedbackEntries.SingleAsync(x => x.Id == data.ControlFeedbackId, ct);
            Assert.True(excluded.ExcludeFromLearning(data.UserId, "Historical replay").IsSuccess);
            await db.SaveChangesAsync(ct);
            await db.CatalogImportBatches.Where(x => x.Id == data.BatchId).ExecuteDeleteAsync(ct);
        }
        var replayResponse = await client.PostAsync(new Uri($"{Root}/rule-set-reports/{report.Id}/evaluation/replay", UriKind.Relative), null, ct);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        var replay = (await replayResponse.Content.ReadFromJsonAsync<EvaluationPage>(ct))!;
        Assert.Equal(EvaluationJson.Serialize(page.Control), EvaluationJson.Serialize(replay.Control));
        Assert.Equal("Historical", replay.Readiness.State);
        var historical = await client.GetAsync(new Uri($"{Root}/rule-set-reports/{report.Id}", UriKind.Relative), ct);
        Assert.Equal(HttpStatusCode.OK, historical.StatusCode);
        using var other = Client(app, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync(new Uri($"{Root}/rule-set-reports/{report.Id}/evaluation", UriKind.Relative), ct)).StatusCode);
        var disable = await client.PostAsJsonAsync($"{Root}/rule-set-switches", Switch(data, report.Id) with { ExpectedSequenceNumber = 1, NewVersionId = null, ReportId = null }, ct);
        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);
    }

    [Theory]
    [InlineData("revoke")]
    [InlineData("exclude")]
    [InlineData("baseline")]
    [InlineData("schema")]
    [InlineData("profile")]
    [InlineData("new-confirmation")]
    public async Task ChangedDependenciesBlockDirectHttpActivation(string change)
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedAsync(ct);
        await using var app = await StartAsync(ct);
        using var client = Client(app, data.UserId);
        var report = await CreateReportAsync(client, data, ct);
        await using (var db = fixture.CreateDbContext())
        {
            switch (change)
            {
                case "revoke":
                    Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/catalog/recognition/training-examples/{data.ControlId}/revoke", new { reason = "Changed" }, ct)).StatusCode);
                    break;
                case "exclude":
                    Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/catalog/recognition/learning-provenance/feedback/{data.ControlFeedbackId}/exclude", new { reason = "Changed" }, ct)).StatusCode);
                    break;
                case "schema":
                    await db.CharacteristicDefinitions.Where(x => x.Id == data.Graph.Definition.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Name, "Changed name"), ct);
                    break;
                case "baseline":
                    db.CatalogDictionaryTerms.Add(ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTerm.Create("16", ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTermKind.Characteristic,
                        data.Graph.Definition.Code, "16", 100, ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTermStatus.Approved,
                        ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTermSource.UserCorrection, data.Graph.Manufacturer.Id, data.Graph.ProductType.Id).Value);
                    await db.SaveChangesAsync(ct);
                    break;
                case "profile":
                    db.CatalogCharacteristicRecognitionProfiles.Add(CatalogCharacteristicRecognitionProfile.Create(data.Graph.ProductType.Id,
                        data.Graph.Definition.Id, CatalogCharacteristicRecognitionStrategyKind.Dictionary, 100, 0.9m, "{}").Value);
                    await db.SaveChangesAsync(ct);
                    break;
                case "new-confirmation":
                    var (feedback, example) = Example(data.Graph, data.UserId, "16 another");
                    db.CatalogRecognitionFeedbackEntries.Add(feedback);
                    db.CatalogRecognitionTrainingExamples.Add(example);
                    db.Entry(example).Property(x => x.IsEvaluationOnly).CurrentValue = true;
                    await db.SaveChangesAsync(ct);
                    break;
            }
        }
        var rejected = await client.PostAsJsonAsync($"{Root}/rule-set-switches", Switch(data, report.Id), ct);
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Contains("evaluation.stale", await rejected.Content.ReadAsStringAsync(ct), StringComparison.Ordinal);
        await using var check = fixture.CreateDbContext();
        Assert.False(await check.CatalogRecognitionRuleSetSwitches.AnyAsync(x => x.ManufacturerId == data.Graph.Manufacturer.Id, ct));
    }

    [Theory]
    [InlineData("legacy", "evaluation.legacy_report")]
    [InlineData("unsupported", "evaluation.unsupported_format")]
    [InlineData("no-control", "evaluation.not_ready")]
    [InlineData("no-improvement", "evaluation.not_ready")]
    public async Task DirectActivationCannotBypassEvaluation(string scenario, string code)
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedAsync(ct);
        await using var app = await StartAsync(ct);
        using var client = Client(app, data.UserId);
        await using var db = fixture.CreateDbContext();
        if (string.Equals(scenario, "no-control", StringComparison.Ordinal))
            await db.CatalogRecognitionTrainingExamples.Where(x => x.Id == data.ControlId).ExecuteDeleteAsync(ct);
        if (string.Equals(scenario, "no-improvement", StringComparison.Ordinal))
        {
            db.CatalogDictionaryTerms.Add(ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTerm.Create("16", ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTermKind.Characteristic,
                data.Graph.Definition.Code, "16", 100, ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTermStatus.Approved,
                ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTermSource.UserCorrection, data.Graph.Manufacturer.Id, data.Graph.ProductType.Id).Value);
            await db.SaveChangesAsync(ct);
        }
        var report = await CreateReportAsync(client, data, ct);
        if (string.Equals(scenario, "legacy", StringComparison.Ordinal))
            await db.CatalogRecognitionRuleSetReports.Where(x => x.Id == report.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.EvaluationJson, (string?)null), ct);
        if (string.Equals(scenario, "unsupported", StringComparison.Ordinal))
            await db.CatalogRecognitionRuleSetReports.Where(x => x.Id == report.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.EvaluationJson, "{\"formatVersion\":999}"), ct);
        var response = await client.PostAsJsonAsync($"{Root}/rule-set-switches", Switch(data, report.Id), ct);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(code, await response.Content.ReadAsStringAsync(ct), StringComparison.Ordinal);
        Assert.False(await db.CatalogRecognitionRuleSetSwitches.AnyAsync(x => x.ManufacturerId == data.Graph.Manufacturer.Id, ct));
    }

    [Fact]
    public async Task ConcurrentActivationsCreateExactlyOneSwitch()
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedAsync(ct);
        await using var app = await StartAsync(ct);
        using var client = Client(app, data.UserId);
        var report = await CreateReportAsync(client, data, ct);
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync($"{Root}/rule-set-switches", Switch(data, report.Id), ct),
            client.PostAsJsonAsync($"{Root}/rule-set-switches", Switch(data, report.Id), ct));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.CatalogRecognitionRuleSetSwitches.CountAsync(x => x.ManufacturerId == data.Graph.Manufacturer.Id, ct));
    }

    [Theory]
    [InlineData("revoke")]
    [InlineData("exclude")]
    [InlineData("baseline")]
    public async Task ActivationWaitsForDependencyCommitBeforeTakingSnapshot(string change)
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedAsync(ct);
        await using var app = await StartAsync(ct);
        using var client = Client(app, data.UserId);
        var report = await CreateReportAsync(client, data, ct);
        await using var db = fixture.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        switch (change)
        {
            case "revoke":
                var example = await db.CatalogRecognitionTrainingExamples.SingleAsync(x => x.Id == data.ControlId, ct);
                Assert.True(example.Revoke(data.UserId, "Concurrent change").IsSuccess);
                break;
            case "exclude":
                var feedback = await db.CatalogRecognitionFeedbackEntries.SingleAsync(x => x.Id == data.ControlFeedbackId, ct);
                Assert.True(feedback.ExcludeFromLearning(data.UserId, "Concurrent change").IsSuccess);
                break;
            case "baseline":
                db.CatalogDictionaryTerms.Add(ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTerm.Create("16", ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTermKind.Characteristic,
                    data.Graph.Definition.Code, "16", 100, ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTermStatus.Approved,
                    ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryTermSource.UserCorrection, data.Graph.Manufacturer.Id, data.Graph.ProductType.Id).Value);
                break;
        }
        await db.SaveChangesAsync(ct);
        // Deliberately overlap HTTP with the open transaction; await the response before disposing the client.
#pragma warning disable CA2025
        var activation = client.PostAsJsonAsync($"{Root}/rule-set-switches", Switch(data, report.Id), ct);
#pragma warning restore CA2025
        await Task.Delay(150, ct);
        Assert.False(activation.IsCompleted);
        await transaction.CommitAsync(ct);
        var response = await activation;
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("evaluation.stale", await response.Content.ReadAsStringAsync(ct), StringComparison.Ordinal);
        Assert.False(await db.CatalogRecognitionRuleSetSwitches.AnyAsync(x => x.ManufacturerId == data.Graph.Manufacturer.Id, ct));
    }

    [Theory]
    [InlineData("foreign", HttpStatusCode.NotFound)]
    [InlineData("blocked", HttpStatusCode.Forbidden)]
    [InlineData("permission", HttpStatusCode.Forbidden)]
    public async Task EveryReportOperationChecksCurrentAccess(string condition, HttpStatusCode expected)
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedAsync(ct);
        await using var app = await StartAsync(ct);
        using var owner = Client(app, data.UserId);
        var report = await CreateReportAsync(owner, data, ct);
        await using var db = fixture.CreateDbContext();
        var user = await db.Users.SingleAsync(x => x.Id == data.UserId, ct);
        switch (condition)
        {
            case "foreign":
                user = TestDataFactory.CreateTechnicalUser(email: $"foreign-evaluation-{Guid.NewGuid():N}@example.com");
                db.Users.Add(user);
                break;
            case "blocked": Assert.True(user.Block().IsSuccess); break;
            case "permission": Assert.True(user.MakeRegular().IsSuccess); break;
        }
        await db.SaveChangesAsync(ct);
        using var client = Client(app, user.Id);
        Assert.Equal(expected, (await client.GetAsync(new Uri($"{Root}/rule-set-reports/{report.Id}/evaluation", UriKind.Relative), ct)).StatusCode);
        Assert.Equal(expected, (await client.PostAsync(new Uri($"{Root}/rule-set-reports/{report.Id}/evaluation/replay", UriKind.Relative), null, ct)).StatusCode);
        Assert.Equal(expected, (await client.PostAsJsonAsync($"{Root}/rule-set-switches", Switch(data, report.Id), ct)).StatusCode);
        // The existing batch ownership contract returns Forbidden, while private report lookups return NotFound.
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync(new Uri($"{Root}/rule-set-versions/{data.VersionId}/reports/batches/{data.BatchId}", UriKind.Relative), null, ct)).StatusCode);
    }

    [Fact]
    public async Task LimitDoesNotSavePartialReportAndPolicyChangeInvalidatesExistingReport()
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedAsync(ct);
        await using (var limited = await StartAsync(ct, "RecognitionEvaluation:MaxExamples", "1"))
        {
            using var client = Client(limited, data.UserId);
            var response = await client.PostAsync(new Uri($"{Root}/rule-set-versions/{data.VersionId}/reports/batches/{data.BatchId}", UriKind.Relative), null, ct);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Contains("evaluation.selection_too_large", await response.Content.ReadAsStringAsync(ct), StringComparison.Ordinal);
            await using var db = fixture.CreateDbContext();
            Assert.False(await db.CatalogRecognitionRuleSetReports.AnyAsync(x => x.RuleSetVersionId == data.VersionId, ct));
        }
        Guid reportId;
        await using (var normal = await StartAsync(ct))
        {
            using var client = Client(normal, data.UserId);
            reportId = (await CreateReportAsync(client, data, ct)).Id;
        }
        await using var stricter = await StartAsync(ct, "RecognitionEvaluation:MinimumControlNames", "2");
        using var changedClient = Client(stricter, data.UserId);
        var rejected = await changedClient.PostAsJsonAsync($"{Root}/rule-set-switches", Switch(data, reportId), ct);
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Contains("evaluation.stale", await rejected.Content.ReadAsStringAsync(ct), StringComparison.Ordinal);
    }

    private async Task<WebApplication> StartAsync(CancellationToken ct, string? setting = null, string? value = null, Action<IServiceCollection>? configure = null, Action<WebApplication>? configureApp = null)
    {
        using var db = fixture.CreateDbContext();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) { ["ConnectionStrings:Database"] = db.Database.GetConnectionString() });
        if (setting is not null) builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) { [setting] = value });
        builder.Services.AddCore();
        builder.Services.AddInfrastructurePostgres(builder.Configuration);
        foreach (var descriptor in builder.Services.Where(d => d.ServiceType == typeof(IHostedService) &&
            (d.ImplementationType == typeof(CatalogRecognitionLearningHostedService) || d.ImplementationType == typeof(CatalogImportCleanupHostedService))).ToArray())
            builder.Services.Remove(descriptor);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserProvider, HttpContextCurrentUserProvider>();
        builder.Services.AddAuthentication("integration").AddScheme<AuthenticationSchemeOptions, IntegrationAuthentication>("integration", _ => { });
        builder.Services.AddAuthorization(options => options.AddPolicy(PermissionPolicy.For(UserPermissionCode.DictionariesManage), policy => policy.RequireAuthenticatedUser()));
        builder.Services.AddControllers().AddApplicationPart(typeof(SwitchCatalogRecognitionRuleSetController).Assembly);
        configure?.Invoke(builder.Services);
        var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        configureApp?.Invoke(app);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync(ct);
        return app;
    }

    private static HttpClient Client(WebApplication app, Guid user)
    {
        var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()), Timeout = TimeSpan.FromSeconds(90) };
        client.DefaultRequestHeaders.Add("X-Test-User", user.ToString());
        return client;
    }

    private static CatalogRecognitionRuleSetSwitchCommand Switch(Seed data, Guid reportId) =>
        new(data.Graph.Manufacturer.Id, data.Graph.ProductType.Id, 0, data.VersionId, reportId, "Checked improvement", true);

    private static async Task<CatalogRecognitionRuleSetReportCreated> CreateReportAsync(HttpClient client, Seed data, CancellationToken ct)
    {
        var response = await client.PostAsync(new Uri($"{Root}/rule-set-versions/{data.VersionId}/reports/batches/{data.BatchId}", UriKind.Relative), null, ct);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync(ct));
        return (await response.Content.ReadFromJsonAsync<CatalogRecognitionRuleSetReportCreated>(ct))!;
    }

    private async Task<Seed> SeedAsync(CancellationToken ct)
    {
        await using var db = fixture.CreateDbContext();
        var graph = PostgreSqlTestDataFactory.CreateCatalogProductGraph();
        var user = TestDataFactory.CreateTechnicalUser(email: $"evaluation-{Guid.NewGuid():N}@example.com");
        db.Users.Add(user);
        db.Manufacturers.Add(graph.Manufacturer);
        db.ProductTypes.Add(graph.ProductType);
        db.CharacteristicDefinitions.Add(graph.Definition);
        var (trainingFeedback, training) = Example(graph, user.Id, "16 training");
        var (controlFeedback, control) = Example(graph, user.Id, "16 control");
        db.CatalogRecognitionFeedbackEntries.AddRange(trainingFeedback, controlFeedback);
        db.CatalogRecognitionTrainingExamples.AddRange(training, control);
        db.Entry(control).Property(x => x.IsEvaluationOnly).CurrentValue = true;
        var draft = CatalogRecognitionLiteralDraft.Create(graph.Manufacturer.Id, graph.ProductType.Id, graph.Definition.Id, "16", "16", CatalogRecognitionLiteralProposalGenerator.Version, 1, user.Id, [training.Id], [training.Id]).Value;
        db.CatalogRecognitionLiteralDrafts.Add(draft);
        var version = CatalogRecognitionRuleSetVersion.Create(new(graph.Manufacturer.Id, graph.ProductType.Id, 1, "Candidate", user.Id, [new(CatalogRecognitionRuleKind.Literal, draft.Id)])).Value;
        db.CatalogRecognitionRuleSetVersions.Add(version);
        var batch = CatalogImportBatch.Create(user.Id, "evaluation.xlsx", "application/octet-stream", [1]).Value;
        Assert.True(batch.AssignProductType(graph.ProductType.Id).IsSuccess);
        Assert.True(batch.RegisterAnalysisResult(1, 1, 0, false).IsSuccess);
        db.CatalogImportBatches.Add(batch);
        var rowData = new CatalogImportNormalizedRowData("16 application", "eval-1", graph.Manufacturer.Name, 10, 1, new Dictionary<string, string>(StringComparer.Ordinal), graph.Manufacturer.Id, ProductTypeId: graph.ProductType.Id);
        db.CatalogImportRows.Add(CatalogImportRow.Create(batch.Id, 2, CatalogImportRowStatus.Valid, "{}", EvaluationJson.Serialize(rowData), "[]", "[]").Value);
        await db.SaveChangesAsync(ct);
        return new(user.Id, graph, batch.Id, version.Id, control.Id, controlFeedback.Id);
    }

    private static (CatalogRecognitionFeedback Feedback, CatalogRecognitionTrainingExample Example) Example(CatalogProductGraph graph, Guid user, string name)
    {
        var feedback = CatalogRecognitionFeedback.Create(name, name.ToUpperInvariant(), graph.Manufacturer.Id, graph.ProductType.Id, graph.ProductType.Code,
            graph.Definition.Id, graph.Definition.Code, null, null, null, null, null, null, CatalogRecognitionFeedbackType.AddedManually, "16", null, null, null, null, null).Value;
        Assert.True(feedback.SetConfirmedSpan(0, 2).IsSuccess);
        Assert.True(feedback.Finalize(CatalogRecognitionLabelQuality.Strong, user, "Technical", true).IsSuccess);
        return (feedback, CatalogRecognitionTrainingExample.Create(feedback, user).Value);
    }

    private sealed record Seed(Guid UserId, CatalogProductGraph Graph, Guid BatchId, Guid VersionId, Guid ControlId, Guid ControlFeedbackId);

    private sealed class IntegrationAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Guid.TryParse(Request.Headers["X-Test-User"], out var user)) return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.ToString())], "integration");
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), "integration")));
        }
    }
}
