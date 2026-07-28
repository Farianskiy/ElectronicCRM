namespace ElectronicService.Core.Catalog.ImportBatches.StartCatalogImportReview;

public sealed record StartCatalogImportReviewCommand(
    Guid BatchId,
    Guid CurrentUserId);