namespace ElectronicService.Core.Catalog.Manufacturers.Resolution;

public sealed record ManufacturerNameRecognitionResult(
    string ProductName,
    ManufacturerNameRecognitionStatus Status,
    ManufacturerNameRecognitionCandidate? SelectedCandidate,
    IReadOnlyList<ManufacturerNameRecognitionCandidate> Candidates)
{
    public bool IsResolved => Status == ManufacturerNameRecognitionStatus.Resolved;

    public bool IsConflict => Status == ManufacturerNameRecognitionStatus.Conflict;

    public static ManufacturerNameRecognitionResult Unresolved(string productName)
    {
        return new ManufacturerNameRecognitionResult(
            productName,
            ManufacturerNameRecognitionStatus.Unresolved,
            null,
            Array.Empty<ManufacturerNameRecognitionCandidate>());
    }

    public static ManufacturerNameRecognitionResult Resolved(
        string productName,
        ManufacturerNameRecognitionCandidate selectedCandidate,
        IReadOnlyList<ManufacturerNameRecognitionCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(selectedCandidate);
        ArgumentNullException.ThrowIfNull(candidates);

        return new ManufacturerNameRecognitionResult(
            productName,
            ManufacturerNameRecognitionStatus.Resolved,
            selectedCandidate,
            candidates);
    }

    public static ManufacturerNameRecognitionResult Conflict(
        string productName,
        IReadOnlyList<ManufacturerNameRecognitionCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return new ManufacturerNameRecognitionResult(
            productName,
            ManufacturerNameRecognitionStatus.Conflict,
            null,
            candidates);
    }
}