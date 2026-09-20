namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionRuleSetVersionListItem(
    Guid Id,
    int VersionNumber,
    string Name,
    int EntryCount,
    DateTime CreatedAtUtc);

public sealed record CatalogRecognitionRuleSetVersionPage(
    IReadOnlyList<CatalogRecognitionRuleSetVersionListItem> Items,
    int Page,
    int PageSize,
    bool HasMore);

public sealed record CatalogRecognitionRuleSetVersionEntryItem(
    int Position,
    int Kind,
    Guid DraftId);

public sealed record CatalogRecognitionRuleSetVersionDetails(
    Guid Id,
    Guid ManufacturerId,
    Guid ProductTypeId,
    int VersionNumber,
    string Name,
    DateTime CreatedAtUtc,
    IReadOnlyList<CatalogRecognitionRuleSetVersionEntryItem> Entries);