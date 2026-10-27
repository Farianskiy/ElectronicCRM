namespace ElectronicService.Core.Catalog.Products.Analogies;

public interface ICatalogProductAnalogyReader
{
    Task<CatalogProductAnalogyCandidateSnapshot> ReadCandidateSnapshotAsync(
        Guid manufacturerId,
        Guid? productTypeId,
        int maximumCandidates,
        CancellationToken cancellationToken = default);
}

public sealed record CatalogProductAnalogyCandidateSnapshot(
    string SnapshotId,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<CatalogProductAnalogyCandidate> Candidates);
