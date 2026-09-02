namespace ElectronicService.Contracts.Catalog.Dictionaries;

public sealed record CatalogDictionaryTermResponse(
    Guid Id,
    Guid? ProductTypeId,
    string Phrase,
    string NormalizedPhrase,
    string Kind,
    string? TargetCode,
    string TargetValue,
    int Priority,
    string Status,
    string Source,
    DateTime CreatedAtUtc,
    DateTime? ApprovedAtUtc,
    DateTime? DisabledAtUtc,
    Guid? DisabledByUserId,
    string? DisabledByUserDisplayName,
    string? DisableReason,
    DateTime? ReactivatedAtUtc,
    Guid? ReactivatedByUserId,
    string? ReactivatedByUserDisplayName);