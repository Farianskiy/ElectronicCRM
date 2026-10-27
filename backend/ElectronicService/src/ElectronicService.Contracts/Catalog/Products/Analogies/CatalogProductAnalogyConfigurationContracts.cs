namespace ElectronicService.Contracts.Catalog.Products.Analogies;

public sealed record CatalogProductAnalogyCharacteristicSettingsContract(
    string Mode,
    int? MinimumSupportingProducts,
    decimal? MinimumSupportRatio);

public sealed record CatalogProductAnalogySettingsContract(
    int MinimumSupportingProducts,
    int MinimumProductsWithCharacteristic,
    decimal MinimumSupportRatio,
    int MaximumAlternativeValues,
    int MaximumCandidatePoolSize,
    IReadOnlyDictionary<string, CatalogProductAnalogyCharacteristicSettingsContract> Characteristics);

public sealed record SaveCatalogProductAnalogyConfigurationRequest(
    string Name,
    CatalogProductAnalogySettingsContract Settings,
    uint? ExpectedVersion = null);

public sealed record CatalogProductAnalogyConfigurationResponse(
    Guid? Id,
    int? VersionNumber,
    string Name,
    string Status,
    string SettingsHash,
    bool IsSystem,
    CatalogProductAnalogySettingsContract Settings,
    DateTime? CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    uint? Version);

public sealed record CatalogProductAnalogyConfigurationWorkspaceResponse(
    CatalogProductAnalogyConfigurationResponse Active,
    IReadOnlyCollection<CatalogProductAnalogyConfigurationResponse> Versions);

public sealed record CatalogProductAnalogyEvaluationReportResponse(
    Guid Id,
    Guid ConfigurationId,
    DateTime CreatedAtUtc,
    int ControlProducts,
    int ControlValues,
    int Improvements,
    int Regressions,
    int CandidateCorrect,
    int CandidateIncorrect,
    bool IsReady,
    bool IsCurrent,
    IReadOnlyCollection<string> BlockingReasons);

public sealed record ActivateCatalogProductAnalogyConfigurationRequest(
    Guid ReportId,
    uint ExpectedVersion,
    bool Confirmed);

public sealed record CatalogProductAnalogyActivationResponse(
    Guid ConfigurationId,
    int VersionNumber,
    string Status,
    DateTime? ActivatedAtUtc);

public sealed record CatalogProductAnalogyEvaluationRowResponse(
    Guid ProductId,
    string ProductName,
    string CharacteristicCode,
    string ExpectedValue,
    string? BaselineValue,
    string? CandidateValue,
    bool BaselineCorrect,
    bool CandidateCorrect);

public sealed record CatalogProductAnalogyEvaluationReportDetailResponse(
    CatalogProductAnalogyEvaluationReportResponse Report,
    IReadOnlyCollection<CatalogProductAnalogyEvaluationRowResponse> Rows);
