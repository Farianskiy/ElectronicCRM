namespace ElectronicService.Core.Catalog.Manufacturers.Resolution;

public sealed record ManufacturerResolutionResult(
    string InputName,
    string NormalizedInputName,
    ManufacturerResolutionStatus Status,
    Guid? ManufacturerId,
    string? ManufacturerName,
    ManufacturerResolutionSource Source,
    Guid? ManufacturerAliasId,
    Guid? ManufacturerNoisePhraseId,
    string? NoiseReason)
{
    public bool IsResolved => Status == ManufacturerResolutionStatus.Resolved;

    public bool IsIgnoredNoise => Status == ManufacturerResolutionStatus.IgnoredNoise;

    public static ManufacturerResolutionResult Resolved(
        string inputName,
        string normalizedInputName,
        Guid manufacturerId,
        string manufacturerName,
        ManufacturerResolutionSource source,
        Guid? manufacturerAliasId)
    {
        return new ManufacturerResolutionResult(
            inputName,
            normalizedInputName,
            ManufacturerResolutionStatus.Resolved,
            manufacturerId,
            manufacturerName,
            source,
            manufacturerAliasId,
            null,
            null);
    }

    public static ManufacturerResolutionResult IgnoredNoise(
        string inputName,
        string normalizedInputName,
        Guid manufacturerNoisePhraseId,
        string? noiseReason)
    {
        return new ManufacturerResolutionResult(
            inputName,
            normalizedInputName,
            ManufacturerResolutionStatus.IgnoredNoise,
            null,
            null,
            ManufacturerResolutionSource.IgnoredNoise,
            null,
            manufacturerNoisePhraseId,
            noiseReason);
    }

    public static ManufacturerResolutionResult Unresolved(string inputName, string normalizedInputName)
    {
        return new ManufacturerResolutionResult(
            inputName,
            normalizedInputName,
            ManufacturerResolutionStatus.Unresolved,
            null,
            null,
            ManufacturerResolutionSource.None,
            null,
            null,
            null);
    }
}