using ElectronicService.Core.Catalog.Products.Analogies;

namespace ElectronicService.Core.UnitTests.Catalog.Products.Analogies;

public sealed class PreviewCatalogProductAnalogiesQueryHandlerTests
{
    [Fact]
    public async Task HandleUsesScopeSnapshotAndReturnsItsIdentity()
    {
        var manufacturerId = Guid.NewGuid();
        var productTypeId = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow.AddMinutes(-1);
        var reader = new StubReader(
            new CatalogProductAnalogyCandidateSnapshot(
                "scope-snapshot-1",
                createdAtUtc,
                []));
        var handler = new PreviewCatalogProductAnalogiesQueryHandler(reader);

        var result = await handler.Handle(
            new CatalogProductAnalogyRequest(
                "NB1 1P 10A B",
                "NB1-10B",
                manufacturerId,
                productTypeId,
                new Dictionary<string, string>(StringComparer.Ordinal),
                MaximumResults: 10),
            TestContext.Current.CancellationToken);

        Assert.Equal("scope-snapshot-1", result.CandidateSnapshotId);
        Assert.Equal(createdAtUtc, result.CandidateSnapshotCreatedAtUtc);
        Assert.Equal(manufacturerId, reader.ManufacturerId);
        Assert.Equal(productTypeId, reader.ProductTypeId);
        Assert.Equal(2000, reader.MaximumCandidates);
        Assert.Equal(1, reader.Calls);
    }

    [Fact]
    public async Task PreparedScopeReusesOneCandidateSnapshotForMultipleRows()
    {
        var manufacturerId = Guid.NewGuid();
        var productTypeId = Guid.NewGuid();
        var reader = new StubReader(
            new CatalogProductAnalogyCandidateSnapshot(
                "batch-snapshot-1",
                DateTime.UtcNow,
                []));
        var handler = new PreviewCatalogProductAnalogiesQueryHandler(reader);

        var scope = await handler.PrepareScopeAsync(
            manufacturerId,
            productTypeId,
            TestContext.Current.CancellationToken);

        var first = PreviewCatalogProductAnalogiesQueryHandler.EvaluatePrepared(
            Request("NB1-63 1P 10A"),
            scope);
        var second = PreviewCatalogProductAnalogiesQueryHandler.EvaluatePrepared(
            Request("NB1-63 1P 16A"),
            scope);

        Assert.Equal(1, reader.Calls);
        Assert.Equal("batch-snapshot-1", first.CandidateSnapshotId);
        Assert.Equal(first.CandidateSnapshotId, second.CandidateSnapshotId);
        Assert.Equal(first.ConfigurationHash, second.ConfigurationHash);

        CatalogProductAnalogyRequest Request(string name) =>
            new(
                name,
                null,
                manufacturerId,
                productTypeId,
                new Dictionary<string, string>(StringComparer.Ordinal));
    }

    private sealed class StubReader(
        CatalogProductAnalogyCandidateSnapshot snapshot)
        : ICatalogProductAnalogyReader
    {
        public int Calls { get; private set; }

        public Guid ManufacturerId { get; private set; }

        public Guid? ProductTypeId { get; private set; }

        public int MaximumCandidates { get; private set; }

        public Task<CatalogProductAnalogyCandidateSnapshot>
            ReadCandidateSnapshotAsync(
                Guid manufacturerId,
                Guid? productTypeId,
                int maximumCandidates,
                CancellationToken cancellationToken = default)
        {
            Calls++;
            ManufacturerId = manufacturerId;
            ProductTypeId = productTypeId;
            MaximumCandidates = maximumCandidates;
            return Task.FromResult(snapshot);
        }
    }
}
