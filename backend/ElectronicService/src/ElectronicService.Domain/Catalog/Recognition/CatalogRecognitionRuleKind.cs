namespace ElectronicService.Domain.Catalog.Recognition;

public enum CatalogRecognitionRuleKind
{
    None = 0,
    Literal = 1,
    NumericCapture = 2,
    MultipleNumericCaptures = 3
}