namespace ElectronicService.Domain.Catalog.Recognition;

public enum CatalogRecognitionCandidateStatus
{
    None = 0,

    Accumulating = 1,

    SuggestionCreated = 2,

    Approved = 3,

    Rejected = 4
}