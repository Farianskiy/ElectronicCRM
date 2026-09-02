namespace ElectronicService.Contracts.Catalog.Manufacturers.Recognition;

public sealed record ManufacturerNameRecognitionPreviewResponse(
    string ProductName,
    string Status,
    bool IsResolved,
    bool IsConflict,
    ManufacturerNameRecognitionCandidateResponse? SelectedCandidate,
    IReadOnlyCollection<ManufacturerNameRecognitionCandidateResponse> Candidates);