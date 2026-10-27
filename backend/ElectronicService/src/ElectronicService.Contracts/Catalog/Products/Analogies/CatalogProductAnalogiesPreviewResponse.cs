namespace ElectronicService.Contracts.Catalog.Products.Analogies;

public sealed record CatalogProductAnalogiesPreviewResponse(
    string CandidateSnapshotId,
    DateTime CandidateSnapshotCreatedAtUtc,
    int EvaluatedCandidatesCount,
    IReadOnlyCollection<CatalogProductAnalogyMatchResponse> Matches,
    int ExcludedCandidatesCount,
    bool ExclusionDiagnosticsTruncated,
    IReadOnlyCollection<CatalogProductAnalogyCandidateExclusionResponse> ExcludedCandidates,
    IReadOnlyCollection<CatalogProductAnalogyCharacteristicProposalResponse> CharacteristicProposals,
    IReadOnlyCollection<CatalogProductAnalogyProductTypeProposalResponse> ProductTypeProposals,
    Guid? ConfigurationId,
    int? ConfigurationVersion,
    string ConfigurationName,
    string ConfigurationHash,
    bool IsSystemConfiguration);

public sealed record CatalogProductAnalogyCandidateExclusionResponse(
    Guid ProductId,
    string Article,
    string Name,
    string ReasonCode,
    string Reason,
    IReadOnlyCollection<string> CandidateFamilyAnchors,
    IReadOnlyCollection<string> SharedNameTokens,
    int MatchingCharacteristicCount,
    int ConflictingCharacteristicCount);

public sealed record CatalogProductAnalogyCharacteristicProposalResponse(
    string CharacteristicCode,
    string ProposedValue,
    int SupportingProductCount,
    int ProductsWithCharacteristicCount,
    decimal SupportRatio,
    bool IsSuggested,
    IReadOnlyCollection<string> BlockingReasons,
    IReadOnlyCollection<Guid> SupportingProductIds,
    IReadOnlyDictionary<string, int> AlternativeValues,
    Guid? FeedbackId,
    string Mode,
    bool IsAutoFillAllowed,
    string? AutoFillBlockingReason);

public sealed record CatalogProductAnalogyProductTypeProposalResponse(
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    int SupportingProductCount,
    int EligibleProductCount,
    decimal SupportRatio,
    bool IsSuggested,
    IReadOnlyCollection<string> BlockingReasons,
    IReadOnlyCollection<Guid> SupportingProductIds);

public sealed record CatalogProductAnalogyMatchResponse(
    Guid ProductId,
    string Article,
    string Name,
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    string MatchedName,
    bool ExactArticleMatch,
    bool ExactNameMatch,
    int SharedNameTokenCount,
    decimal QueryNameTokenCoverage,
    int MatchingCharacteristicCount,
    int ConflictingCharacteristicCount,
    IReadOnlyCollection<string> SharedNameTokens,
    IReadOnlyCollection<string> MatchingCharacteristicCodes,
    IReadOnlyCollection<string> ConflictingCharacteristicCodes,
    IReadOnlyDictionary<string, string> Characteristics,
    string? MatchedFamilyAnchor,
    IReadOnlyCollection<string> CandidateFamilyAnchors);
