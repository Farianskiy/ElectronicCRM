using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Users;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.TestCommon;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog;

public sealed partial class RecognitionEvaluationReleaseTests
{
    private static string WorkspaceUrl(Seed data, string section, string extra = "") =>
        $"/api/catalog/recognition/learning-workspace/{section}?manufacturerId={data.Graph.Manufacturer.Id}&productTypeId={data.Graph.ProductType.Id}{extra}";

    private static async Task<JsonElement> WorkspaceJson(HttpClient client, Seed data, string section, CancellationToken ct, string extra = "")
    {
        var response = await client.GetAsync(new Uri(WorkspaceUrl(data, section, extra), UriKind.Relative), ct);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync(ct));
        return await response.Content.ReadFromJsonAsync<JsonElement>(ct);
    }

    [Fact]
    public async Task WorkspaceReadsScopeWithoutGeneratingOrEvaluatingAndPaginatesVersions()
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedAsync(ct);
        await using var db = fixture.CreateDbContext();
        var draft = await db.CatalogRecognitionLiteralDrafts.SingleAsync(x => x.ManufacturerId == data.Graph.Manufacturer.Id, ct);
        for (var number = 2; number <= 23; number++)
            db.CatalogRecognitionRuleSetVersions.Add(CatalogRecognitionRuleSetVersion.Create(new(data.Graph.Manufacturer.Id, data.Graph.ProductType.Id, number, $"Version {number}", data.UserId, [new(CatalogRecognitionRuleKind.Literal, draft.Id)])).Value);
        await db.SaveChangesAsync(ct);
        await using var app = await StartAsync(ct);
        using var client = Client(app, data.UserId);
        var overview = await WorkspaceJson(client, data, "overview", ct);
        Assert.Equal(1, overview.GetProperty("trainingExamples").GetInt32());
        Assert.Equal(1, overview.GetProperty("controlExamples").GetInt32());
        Assert.Equal(23, overview.GetProperty("versions").GetInt32());
        var first = await WorkspaceJson(client, data, "versions", ct);
        var second = await WorkspaceJson(client, data, "versions", ct, "&page=2");
        Assert.Equal(23, first.GetProperty("totalCount").GetInt32());
        Assert.Equal(20, first.GetProperty("items").GetArrayLength());
        Assert.Equal(3, second.GetProperty("items").GetArrayLength());
        Assert.Empty(first.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).Intersect(second.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid())));
        foreach (var section in new[] { "literal", "integer", "multi", "suggestions", "batches", "ruleReports", "dictionaryReports", "examples" })
            await WorkspaceJson(client, data, section, ct);
        Assert.False(await db.CatalogRecognitionRuleSetReports.AnyAsync(x => x.CreatedByUserId == data.UserId, ct));
        Assert.False(await db.Set<CatalogDictionaryEvaluationReport>().AnyAsync(x => x.CreatedByUserId == data.UserId, ct));
        Assert.False(await db.CatalogRecognitionRuleSetSwitches.AnyAsync(x => x.ManufacturerId == data.Graph.Manufacturer.Id, ct));
        Assert.Single(await db.CatalogRecognitionLiteralDrafts.Where(x => x.ManufacturerId == data.Graph.Manufacturer.Id).ToListAsync(ct));
    }

    [Fact]
    public async Task WorkspaceKeepsPrivateReportsExamplesAndBatchesPrivateAndRejectsCrossScopeIds()
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedDictionaryAsync(ct);
        var foreign = await SeedAsync(ct);
        await using var app = await StartAsync(ct);
        using var owner = Client(app, data.Base.UserId);
        var ruleReport = await CreateReportAsync(owner, data.Base, ct);
        var dictionaryReport = await CreateDictionaryReportAsync(owner, data.Command, ct);
        using var client = Client(app, foreign.UserId);
        foreach (var section in new[] { "examples", "ruleReports", "dictionaryReports" })
            Assert.Equal(0, (await WorkspaceJson(client, data.Base, section, ct)).GetProperty("totalCount").GetInt32());
        var overview = await WorkspaceJson(client, data.Base, "overview", ct);
        Assert.Equal(0, overview.GetProperty("trainingExamples").GetInt32());
        Assert.Equal(0, overview.GetProperty("controlExamples").GetInt32());
        foreach (var (section, id) in new[] { ("examples", data.Base.ControlId), ("ruleReports", ruleReport.Id), ("dictionaryReports", dictionaryReport), ("batches", data.Base.BatchId) })
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(new Uri(WorkspaceUrl(data.Base, section, $"&id={id}"), UriKind.Relative), ct)).StatusCode);
        foreach (var (section, id) in new[] { ("versions", data.Base.VersionId), ("suggestions", data.Command.SuggestionId), ("ruleReports", ruleReport.Id) })
            Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync(new Uri(WorkspaceUrl(foreign, section, $"&id={id}"), UriKind.Relative), ct)).StatusCode);
        var shared = await WorkspaceJson(client, data.Base, "suggestions", ct);
        Assert.Equal(1, shared.GetProperty("totalCount").GetInt32());
        Assert.Empty(shared.GetProperty("items")[0].GetProperty("evidenceExamples").EnumerateArray());
    }

    [Theory]
    [InlineData("blocked")]
    [InlineData("override")]
    public async Task WorkspaceEnforcesEffectiveAccessEverywhere(string condition)
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedAsync(ct);
        await using var app = await StartAsync(ct);
        using var client = Client(app, data.UserId);
        await using var db = fixture.CreateDbContext();
        if (string.Equals(condition, "blocked", StringComparison.Ordinal)) Assert.True((await db.Users.SingleAsync(x => x.Id == data.UserId, ct)).Block().IsSuccess);
        else db.UserPermissionOverrides.Add(UserPermissionOverride.Create(data.UserId, UserPermissionCode.DictionariesManage, false));
        await db.SaveChangesAsync(ct);
        foreach (var section in new[] { "overview", "versions", "literal", "integer", "multi", "suggestions", "batches", "ruleReports", "dictionaryReports", "examples" })
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(new Uri(WorkspaceUrl(data, section), UriKind.Relative), ct)).StatusCode);
    }

    [Fact]
    public async Task WorkspaceTracksReleaseAndRetainsHistoryAfterCleanupWithoutInventingReadiness()
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedAsync(ct);
        await using var app = await StartAsync(ct);
        using var client = Client(app, data.UserId);
        var version = (await WorkspaceJson(client, data, "versions", ct)).GetProperty("items")[0].GetProperty("id").GetGuid();
        Assert.Equal(data.VersionId, version);
        Assert.Equal(data.BatchId, (await WorkspaceJson(client, data, "batches", ct)).GetProperty("items")[0].GetProperty("id").GetGuid());
        var report = await CreateReportAsync(client, data, ct);
        var reportList = await WorkspaceJson(client, data, "ruleReports", ct, $"&parentId={version}");
        Assert.Contains("актуальность не проверена", reportList.GetProperty("items")[0].GetProperty("label").GetString(), StringComparison.Ordinal);
        Assert.False(reportList.GetProperty("items")[0].TryGetProperty("ready", out _));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{Root}/rule-set-switches", Switch(data, report.Id), ct)).StatusCode);
        var overview = await WorkspaceJson(client, data, "overview", ct);
        Assert.Equal(version, overview.GetProperty("activeVersionId").GetGuid());
        Assert.Equal(1, overview.GetProperty("sequenceNumber").GetInt32());
        await using var db = fixture.CreateDbContext();
        await db.CatalogImportBatches.Where(x => x.Id == data.BatchId).ExecuteDeleteAsync(ct);
        var history = await WorkspaceJson(client, data, "ruleReports", ct, $"&id={report.Id}");
        Assert.False(history.GetProperty("items")[0].GetProperty("batchAvailable").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(new Uri($"{Root}/rule-set-reports/{report.Id}/evaluation", UriKind.Relative), ct)).StatusCode);
        Assert.Equal(0, (await WorkspaceJson(client, data, "batches", ct)).GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task WorkspaceDistinguishesRevocationControlAndSourceExclusionAndFiltersDictionary()
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedDictionaryAsync(ct);
        await using var app = await StartAsync(ct);
        using var client = Client(app, data.Base.UserId);
        await using var db = fixture.CreateDbContext();
        var example = await db.CatalogRecognitionTrainingExamples.SingleAsync(x => x.Id == data.Base.ControlId, ct);
        Assert.True(example.Revoke(data.Base.UserId, "Withdraw confirmation").IsSuccess);
        var source = await db.CatalogRecognitionFeedbackEntries.SingleAsync(x => x.Id == data.Base.ControlFeedbackId, ct);
        Assert.True(source.ExcludeFromLearning(data.Base.UserId, "Exclude source").IsSuccess);
        await db.SaveChangesAsync(ct);
        var overview = await WorkspaceJson(client, data.Base, "overview", ct);
        Assert.Equal(1, overview.GetProperty("revokedExamples").GetInt32());
        Assert.Equal(1, overview.GetProperty("excludedSources").GetInt32());
        Assert.Equal(0, (await WorkspaceJson(client, data.Base, "suggestions", ct, "&status=Approved")).GetProperty("totalCount").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync($"{DictionaryRoot}/{data.Command.SuggestionId}/reject", new { reviewComment = "Не использовать" }, ct)).StatusCode);
        Assert.Equal(1, (await WorkspaceJson(client, data.Base, "suggestions", ct, "&status=Rejected")).GetProperty("totalCount").GetInt32());
        Assert.Equal(0, (await WorkspaceJson(client, data.Base, "suggestions", ct, "&status=Pending")).GetProperty("totalCount").GetInt32());
    }

    [Theory]
    [InlineData("&page=0")]
    [InlineData("&page=10001")]
    [InlineData("&status=Unknown")]
    public async Task WorkspaceRejectsUnboundedOrInvalidFilters(string extra)
    {
        var ct = TestContext.Current.CancellationToken;
        var data = await SeedAsync(ct);
        await using var app = await StartAsync(ct);
        using var client = Client(app, data.UserId);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(new Uri(WorkspaceUrl(data, "suggestions", extra), UriKind.Relative), ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(new Uri("/api/catalog/recognition/learning-workspace/overview", UriKind.Relative), ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(new Uri($"/api/catalog/recognition/learning-workspace/overview?manufacturerId={Guid.NewGuid()}&productTypeId={data.Graph.ProductType.Id}", UriKind.Relative), ct)).StatusCode);
    }
}
