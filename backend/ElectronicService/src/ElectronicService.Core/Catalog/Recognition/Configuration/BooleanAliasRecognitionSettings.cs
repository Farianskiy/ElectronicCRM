namespace ElectronicService.Core.Catalog.Recognition.Configuration;

public sealed record BooleanAliasRecognitionSettings(
    IReadOnlyCollection<string> TrueAliases,
    IReadOnlyCollection<string> FalseAliases);