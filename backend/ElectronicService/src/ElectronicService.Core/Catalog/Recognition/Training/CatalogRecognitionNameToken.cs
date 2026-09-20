namespace ElectronicService.Core.Catalog.Recognition.Training;

public enum CatalogRecognitionNameTokenKind
{
    Letters,
    Digits,
    Whitespace,
    Separator
}

public sealed record CatalogRecognitionNameToken(
    CatalogRecognitionNameTokenKind Kind,
    string Text,
    int Start,
    int Length);