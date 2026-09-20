using System.ComponentModel.DataAnnotations;

namespace ElectronicService.Contracts.Catalog.Recognition;

public sealed record CreateCatalogRecognitionRuleSetEntryRequest(
    [Range(1, 3)] int Kind,
    Guid DraftId);

public sealed record CreateCatalogRecognitionRuleSetVersionRequest(
    Guid ManufacturerId,
    Guid ProductTypeId,
    [Required, StringLength(200)] string Name,
    [Required, MinLength(1), MaxLength(100)]
    IReadOnlyList<CreateCatalogRecognitionRuleSetEntryRequest> Entries);

public sealed record CreateCatalogRecognitionRuleSetVersionResponse(
    Guid Id,
    int VersionNumber);