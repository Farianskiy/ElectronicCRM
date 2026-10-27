using System.Text.Json;

namespace ElectronicService.Core.Catalog.Products.Analogies;

public sealed record CatalogProductAnalogyEvidenceSnapshot(
    string AlgorithmVersion,
    string CandidateSnapshotId,
    DateTime CandidateSnapshotCreatedAtUtc,
    int SupportingProductCount,
    int ProductsWithCharacteristicCount,
    decimal SupportRatio,
    IReadOnlyCollection<Guid> SupportingProductIds,
    IReadOnlyDictionary<string, int> AlternativeValues,
    Guid? ConfigurationId,
    int? ConfigurationVersion,
    string ConfigurationHash,
    string? RejectionReason,
    string? FamilyAnchor = null);

public static class CatalogProductAnalogyEvidence
{
    public const string AlgorithmVersion = "catalog-analogy-v3";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public static string Serialize(
        CatalogProductAnalogyResult result,
        CatalogProductAnalogyCharacteristicProposal proposal,
        string? rejectionReason = null)
    {
        return JsonSerializer.Serialize(
            new CatalogProductAnalogyEvidenceSnapshot(
                AlgorithmVersion,
                result.CandidateSnapshotId ?? string.Empty,
                result.CandidateSnapshotCreatedAtUtc ?? DateTime.UtcNow,
                proposal.SupportingProductCount,
                proposal.ProductsWithCharacteristicCount,
                proposal.SupportRatio,
                proposal.SupportingProductIds,
                proposal.AlternativeValues,
                result.ConfigurationId,
                result.ConfigurationVersion,
                result.ConfigurationHash,
                rejectionReason?.Trim(),
                result.Matches
                    .Where(match => proposal.SupportingProductIds.Contains(match.ProductId))
                    .Select(match => match.MatchedFamilyAnchor)
                    .FirstOrDefault(anchor => !string.IsNullOrWhiteSpace(anchor))),
            JsonOptions);
    }
}
