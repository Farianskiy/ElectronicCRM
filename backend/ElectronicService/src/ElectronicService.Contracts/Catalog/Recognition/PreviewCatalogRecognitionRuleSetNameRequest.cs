using System.ComponentModel.DataAnnotations;

namespace ElectronicService.Contracts.Catalog.Recognition;

public sealed record PreviewCatalogRecognitionRuleSetNameRequest(
    Guid ManufacturerId,
    Guid ProductTypeId,
    [Required, StringLength(2000)] string ProductName);