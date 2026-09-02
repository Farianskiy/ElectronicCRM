namespace ElectronicService.Core.Catalog.Recognition.Matching;

internal sealed record NumericWithUnitRecognitionMatch(
    string RawValue,
    string RawNumericValue,
    int StartIndex,
    int Length);