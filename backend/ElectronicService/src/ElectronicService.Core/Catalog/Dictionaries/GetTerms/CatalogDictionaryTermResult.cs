namespace ElectronicService.Core.Catalog.Dictionaries.GetTerms;

public sealed record CatalogDictionaryTermResult(
    Guid Id,
    Guid? ManufacturerId,
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