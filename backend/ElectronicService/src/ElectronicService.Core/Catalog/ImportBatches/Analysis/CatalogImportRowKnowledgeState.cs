namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public enum CatalogImportRowKnowledgeState
{
    Strict,
    CatalogAnalogy,
    AwaitingConfirmation,
    Conflict,
    RequiredMissing
}
