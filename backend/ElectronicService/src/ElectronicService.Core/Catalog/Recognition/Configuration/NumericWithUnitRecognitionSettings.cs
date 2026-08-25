namespace ElectronicService.Core.Catalog.Recognition.Configuration;

public sealed record NumericWithUnitRecognitionSettings(
    IReadOnlyCollection<string> Units,
    decimal Minimum,
    decimal Maximum,
    bool AllowDecimal);