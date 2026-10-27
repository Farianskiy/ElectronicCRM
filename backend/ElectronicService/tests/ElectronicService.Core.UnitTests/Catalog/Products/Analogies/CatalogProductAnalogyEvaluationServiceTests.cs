using ElectronicService.Core.Catalog.Products.Analogies;
using ElectronicService.Domain.Catalog.Products;

namespace ElectronicService.Core.UnitTests.Catalog.Products.Analogies;

public sealed class CatalogProductAnalogyEvaluationServiceTests
{
    [Fact]
    public async Task EvaluateAndActivateImprovedConfiguration()
    {
        var manufacturerId = Guid.CreateVersion7();
        var productTypeId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var settings = CatalogProductAnalogySettings.SystemDefault with
        {
            MinimumSupportingProducts = 2,
            MinimumProductsWithCharacteristic = 2
        };
        var encoded = CatalogProductAnalogySettingsCodec.Normalize(settings).Value;
        var configuration = CatalogProductAnalogyConfiguration.CreateDraft(
            manufacturerId, productTypeId, 1, "Два согласованных аналога",
            encoded.Json, encoded.Hash, userId).Value;
        var candidates = Enumerable.Range(1, 3).Select(index =>
            new CatalogProductAnalogyCandidate(Guid.CreateVersion7(), $"NB1-{index}",
                "CHINT NB1 1P 16A", manufacturerId, productTypeId,
                "BREAKER", "Автомат", [],
                new Dictionary<string, string>(StringComparer.Ordinal) { ["CURVE"] = "B" },
                false)).ToArray();
        var repository = new Repository(configuration);
        var reader = new Reader(candidates);
        var service = new CatalogProductAnalogyEvaluationService(repository, reader,
            new CatalogProductAnalogyConfigurationResolver(repository));

        var report = await service.EvaluateAsync(configuration.Id, userId,
            TestContext.Current.CancellationToken);

        Assert.True(report.IsSuccess);
        Assert.True(report.Value.IsReady);
        Assert.Equal(3, report.Value.Improvements);
        Assert.Equal(CatalogProductAnalogyConfigurationStatus.Evaluated, configuration.Status);

        var activated = await service.ActivateAsync(configuration.Id,
            report.Value.Id, configuration.Version,
            TestContext.Current.CancellationToken);

        Assert.True(activated.IsSuccess);
        Assert.Equal(CatalogProductAnalogyConfigurationStatus.Active, configuration.Status);
    }

    private sealed class Reader(IReadOnlyCollection<CatalogProductAnalogyCandidate> candidates)
        : ICatalogProductAnalogyReader
    {
        public Task<CatalogProductAnalogyCandidateSnapshot> ReadCandidateSnapshotAsync(
            Guid manufacturerId, Guid? productTypeId, int maximumCandidates,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CatalogProductAnalogyCandidateSnapshot(
                "stable-snapshot", DateTime.UnixEpoch, candidates));
    }

    private sealed class Repository(CatalogProductAnalogyConfiguration configuration)
        : ICatalogProductAnalogyConfigurationRepository
    {
        private readonly List<CatalogProductAnalogyEvaluationReport> _reports = [];
        public Task<CatalogProductAnalogyConfiguration?> GetActiveAsync(Guid manufacturerId, Guid productTypeId, CancellationToken cancellationToken = default) => Task.FromResult<CatalogProductAnalogyConfiguration?>(null);
        public Task<CatalogProductAnalogyConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<CatalogProductAnalogyConfiguration?>(id == configuration.Id ? configuration : null);
        public Task<IReadOnlyCollection<CatalogProductAnalogyConfiguration>> ListAsync(Guid manufacturerId, Guid productTypeId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<CatalogProductAnalogyConfiguration>>([configuration]);
        public Task<int> GetNextVersionNumberAsync(Guid manufacturerId, Guid productTypeId, CancellationToken cancellationToken = default) => Task.FromResult(2);
        public void Add(CatalogProductAnalogyConfiguration configuration) { }
        public void Add(CatalogProductAnalogyEvaluationReport report) => _reports.Add(report);
        public Task<IReadOnlyCollection<CatalogProductAnalogyEvaluationReport>> ListReportsAsync(Guid configurationId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<CatalogProductAnalogyEvaluationReport>>(_reports.ToArray());
        public Task<CatalogProductAnalogyEvaluationReport?> GetReportAsync(Guid reportId, CancellationToken cancellationToken = default) => Task.FromResult(_reports.SingleOrDefault(x => x.Id == reportId));
        public Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> TryActivateAsync(CatalogProductAnalogyConfiguration configuration, uint expectedVersion, CancellationToken cancellationToken = default)
        {
            var result = configuration.Activate();
            return Task.FromResult(result.IsSuccess);
        }
    }
}
