using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using ElectronicService.Infrastructure.Postgres.Catalog.Repositories;
using ElectronicService.TestCommon;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed class CatalogImportReanalysisPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ReanalysisPersistsDetachedRetainedRowWithSameIdentity()
    {
        await using var context = fixture.CreateDbContext();
        var cancellationToken = TestContext.Current.CancellationToken;
        var user = TestDataFactory.CreateTechnicalUser(
            email: $"reanalysis-{Guid.NewGuid():N}@example.com");
        context.Users.Add(user);
        var batchResult = CatalogImportBatch.Create(user.Id, "test.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", [1]);
        Assert.True(batchResult.IsSuccess);
        var batch = batchResult.Value;
        context.CatalogImportBatches.Add(batch);
        const string rawJson = "{\"2\":\"Автомат\",\"1\":\"179635\"}";
        var rowResult = CatalogImportRow.Create(batch.Id, 2, CatalogImportRowStatus.Error,
            rawJson, "{}", "[]", "[]");
        Assert.True(rowResult.IsSuccess);
        context.CatalogImportRows.Add(rowResult.Value);
        Assert.True(batch.RegisterAnalysisResult(1, 0, 1, false).IsSuccess);
        await context.SaveChangesAsync(cancellationToken);

        var detached = await context.CatalogImportRows.AsNoTracking()
            .SingleAsync(row => row.Id == rowResult.Value.Id, cancellationToken);
        context.Entry(rowResult.Value).State = EntityState.Detached;
        var generated = CatalogImportRow.Create(batch.Id, 2, CatalogImportRowStatus.Error,
            rawJson, "{}", "[]", "[]");
        Assert.True(generated.IsSuccess);
        var prepared = CatalogImportReanalysisRows.Prepare([generated.Value], [detached]);
        Assert.True(prepared.IsSuccess);
        Assert.True(detached.ReplaceValidationResult(CatalogImportRowStatus.Valid,
            "{\"characteristics\":{\"curve\":\"D\"}}", "[]", "[]").IsSuccess);
        Assert.True(batch.RegisterAnalysisResult(1, 1, 0, false).IsSuccess);

        var repository = new CatalogImportBatchRepository(context);
        Assert.True(await repository.ReplaceAnalysisAsync(batch, [], prepared.Value, cancellationToken));
        await using var verification = fixture.CreateDbContext();
        var persisted = await verification.CatalogImportRows.AsNoTracking()
            .SingleAsync(row => row.BatchId == batch.Id, cancellationToken);
        Assert.Equal(detached.Id, persisted.Id);
        Assert.Equal(CatalogImportRowStatus.Valid, persisted.Status);
        Assert.Contains("\"D\"", persisted.NormalizedDataJson, StringComparison.Ordinal);
    }
}
