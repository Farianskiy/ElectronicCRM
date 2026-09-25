using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.ImportBatches.AnalyzeCatalogImportBatch;
using ElectronicService.Core.Catalog.Manufacturers.Resolution;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Preview;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.IntegrationTests.Data;
using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using ElectronicService.Infrastructure.Postgres;
using ElectronicService.TestCommon;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed class CatalogEffectiveRecognitionAnalysisTests(PostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task HandlerPersistsEffectiveRecognitionAndExplainsFirstRepeatedAndMixedAnalysis(bool repeated, bool mixed)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var setup = fixture.CreateDbContext();
        var graph = PostgreSqlTestDataFactory.CreateCatalogProductGraph();
        var otherType = TestDataFactory.CreateProductType($"OTHER_{Guid.NewGuid():N}", "Other");
        TestDataFactory.AddCharacteristic(otherType, graph.Definition);
        var user = TestDataFactory.CreateTechnicalUser(email: $"recognition-{Guid.NewGuid():N}@example.com");
        setup.Users.Add(user);
        setup.Manufacturers.Add(graph.Manufacturer);
        setup.CharacteristicDefinitions.Add(graph.Definition);
        setup.ProductTypes.AddRange(graph.ProductType, otherType);
        var batch = CatalogImportBatch.Create(user.Id, "test.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", [1]).Value;
        if (!mixed)
        {
            Assert.True(batch.AssignProductType(graph.ProductType.Id).IsSuccess);
        }

        setup.CatalogImportBatches.Add(batch);
        var analyzer = new PreparedWorkbook(graph, otherType, mixed);
        Guid? retainedId = null;
        if (repeated)
        {
            var saved = analyzer.Rows(batch.Id, manual: true);
            retainedId = saved[0].Id;
            setup.CatalogImportRows.AddRange(saved);
            Assert.True(batch.RegisterAnalysisResult(saved.Length, saved.Length, 0, false).IsSuccess);
        }

        await setup.SaveChangesAsync(ct);
        var rules = new ActiveRules(graph, otherType);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCore();
        services.AddInfrastructurePostgres(new ConfigurationBuilder().Build());
        // Override only external storage configuration and prepared workbook input; use the real handler and repositories.
        services.AddScoped(_ => fixture.CreateDbContext());
        services.AddSingleton<ICatalogImportWorkbookAnalyzer>(analyzer);
        services.AddSingleton<ICatalogRecognitionActiveRuleSetReader>(rules);
        services.AddSingleton<ICatalogRecognitionRuleSetExecutionReader>(rules);
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<AnalyzeCatalogImportBatchCommandHandler>()
            .Handle(new(batch.Id, user.Id, mixed ? null : graph.ProductType.Id), ct);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        var expectedRows = mixed ? 2 : 1;
        Assert.Equal(expectedRows, result.Value.ProductNameExplanation.RowsWithEvidenceCount);
        Assert.Equal(expectedRows, result.Value.RecognitionShadow!.RowsWithRecognition);
        Assert.All(result.Value.RecognitionShadow.EvidenceRows, row => Assert.Equal("16", Assert.Single(row.Evidence).TargetValue));
        Assert.Equal(1, rules.Captures);
        await using var verify = fixture.CreateDbContext();
        var persisted = await verify.CatalogImportRows.Where(row => row.BatchId == batch.Id).OrderBy(row => row.RowNumber).ToArrayAsync(ct);
        Assert.Equal(expectedRows, persisted.Length);
        if (retainedId.HasValue)
        {
            Assert.Equal(retainedId.Value, persisted[0].Id);
        }

        foreach (var row in persisted)
        {
            var data = JsonSerializer.Deserialize<CatalogImportNormalizedRowData>(row.NormalizedDataJson, JsonOptions)!;
            Assert.Equal(repeated ? "25" : "16", data.Characteristics[graph.Definition.Id.ToString()]);
            Assert.Equal(repeated ? CatalogImportCharacteristicValueSource.Manual : CatalogImportCharacteristicValueSource.Recognition,
                data.CharacteristicOrigins![graph.Definition.Id.ToString()].Source);
        }

        await using var previewScope = provider.CreateAsyncScope();
        var preview = await previewScope.ServiceProvider.GetRequiredService<PreviewCatalogProductNameRecognitionQueryHandler>()
            .Handle(new("learned", graph.ProductType.Code, graph.Manufacturer.Id), ct);
        Assert.True(preview.IsSuccess);
        Assert.Equal("16", Assert.Single(preview.Value.RecognitionResult.Characteristics).NormalizedValue);
        Assert.NotNull(preview.Value.ActiveRuleSetVersionId);
    }

    private sealed class PreparedWorkbook(CatalogProductGraph graph, ProductType otherType, bool mixed) : ICatalogImportWorkbookAnalyzer
    {
        public CatalogImportRow[] Rows(Guid batchId, bool manual)
        {
            var types = mixed ? new[] { graph.ProductType, otherType } : [graph.ProductType];
            return types.Select((type, index) =>
            {
                var key = graph.Definition.Id.ToString();
                var data = new CatalogImportNormalizedRowData("learned", $"article-{index}", graph.Manufacturer.Name, null, null,
                    manual ? new Dictionary<string, string>(StringComparer.Ordinal) { [key] = "25" } : new Dictionary<string, string>(StringComparer.Ordinal),
                    graph.Manufacturer.Id, CharacteristicOrigins: manual ? new Dictionary<string, CatalogImportCharacteristicValueOrigin>(StringComparer.Ordinal) { [key] = CatalogImportCharacteristicValueOrigin.FromManual() } : null,
                    ProductTypeId: type.Id, ProductTypeResolutionSource: "Manual");
                return CatalogImportRow.Create(batchId, index + 2, CatalogImportRowStatus.Valid, "{\"1\":\"learned\"}",
                    JsonSerializer.Serialize(data, JsonOptions), "[]", "[]").Value;
            }).ToArray();
        }

        public Result<CatalogImportWorkbookAnalysis, DomainError> Analyze(Guid batchId, ReadOnlyMemory<byte> workbookContent,
            ProductType? productType, IReadOnlyCollection<CharacteristicDefinition> characteristicDefinitions,
            ManufacturerResolutionIndex manufacturerResolutionIndex, IReadOnlyCollection<CatalogImportColumn> existingColumns,
            CancellationToken cancellationToken = default)
        {
            var rows = Rows(batchId, manual: false);
            return new CatalogImportWorkbookAnalysis([], rows, false, rows.Length, 0, new(0, 0, 0, 0, 0, []));
        }
    }

    private sealed class ActiveRules : ICatalogRecognitionActiveRuleSetReader, ICatalogRecognitionRuleSetExecutionReader
    {
        private readonly CatalogRecognitionRuleSetExecutionSnapshot[] _versions;
        public int Captures { get; private set; }

        public ActiveRules(CatalogProductGraph graph, ProductType otherType)
        {
            _versions = new[] { graph.ProductType, otherType }.Select(type => new CatalogRecognitionRuleSetExecutionSnapshot(
                Guid.NewGuid(), graph.Manufacturer.Id, type.Id, 1,
                [new(Guid.NewGuid(), graph.Definition.Id, "literal-v1", "learned", "16")], [], [])).ToArray();
        }

        public Task<IReadOnlyCollection<CatalogRecognitionRuleSetState>> CaptureForRunAsync(CancellationToken cancellationToken = default)
        {
            Captures++;
            return Task.FromResult<IReadOnlyCollection<CatalogRecognitionRuleSetState>>(_versions.Select(State).ToArray());
        }

        private static CatalogRecognitionRuleSetState State(CatalogRecognitionRuleSetExecutionSnapshot version)
            => new(version.ManufacturerId, version.ProductTypeId, 1, null, version.VersionId, null, null);

        public Task<Result<CatalogRecognitionRuleSetExecutionSnapshot, DomainError>> ReadAsync(Guid versionId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success<CatalogRecognitionRuleSetExecutionSnapshot, DomainError>(_versions.Single(v => v.VersionId == versionId)));

        public Task<Result<CatalogRecognitionRuleSetState, DomainError>> GetStateAsync(Guid manufacturerId, Guid productTypeId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success<CatalogRecognitionRuleSetState, DomainError>(State(_versions.Single(v => v.ManufacturerId == manufacturerId && v.ProductTypeId == productTypeId))));

        public Task<Result<CatalogRecognitionActiveRuleSet, DomainError>> LoadAsync(Guid manufacturerId, Guid productTypeId, CancellationToken cancellationToken = default)
        {
            var version = _versions.Single(v => v.ManufacturerId == manufacturerId && v.ProductTypeId == productTypeId);
            return Task.FromResult(Result.Success<CatalogRecognitionActiveRuleSet, DomainError>(new(State(version), version)));
        }
    }
}
