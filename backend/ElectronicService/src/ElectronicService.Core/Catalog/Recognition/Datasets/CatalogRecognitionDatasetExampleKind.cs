namespace ElectronicService.Core.Catalog.Recognition.Datasets;

public enum CatalogRecognitionDatasetExampleKind
{
    None = 0,

    AcceptedSpan = 1,

    AcceptedValue = 2,

    CorrectedValue = 3,

    RejectedSpan = 4,

    RejectedValue = 5,

    ManualValue = 6,

    ConflictResolution = 7
}