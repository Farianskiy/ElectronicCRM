namespace ElectronicService.Domain.Catalog.Recognition;

public enum CatalogRecognitionFeedbackType
{
    None = 0,

    Accepted = 1,

    Corrected = 2,

    Rejected = 3,

    AddedManually = 4,

    ConflictResolved = 5
}