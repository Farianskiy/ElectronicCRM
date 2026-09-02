namespace ElectronicService.Contracts.Catalog.Manufacturers.Recognition;

public sealed record ManufacturerNameRecognitionCandidateResponse(
    Guid ManufacturerId,
    string ManufacturerName,
    string RawValue,
    string NormalizedValue,
    decimal Confidence,
    string Source,
    Guid? ManufacturerAliasId,
    int StartIndex,
    int Length,
    int EndIndex);