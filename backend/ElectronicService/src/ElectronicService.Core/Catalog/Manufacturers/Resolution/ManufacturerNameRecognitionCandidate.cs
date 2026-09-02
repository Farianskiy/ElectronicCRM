namespace ElectronicService.Core.Catalog.Manufacturers.Resolution;

public sealed record ManufacturerNameRecognitionCandidate(
    Guid ManufacturerId,
    string ManufacturerName,
    string RawValue,
    string NormalizedValue,
    decimal Confidence,
    ManufacturerResolutionSource Source,
    Guid? ManufacturerAliasId,
    int StartIndex,
    int Length)
{
    public int EndIndex => StartIndex + Length;
}