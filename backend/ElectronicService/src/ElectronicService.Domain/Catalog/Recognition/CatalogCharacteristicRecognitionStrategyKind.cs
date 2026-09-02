namespace ElectronicService.Domain.Catalog.Recognition;

public enum CatalogCharacteristicRecognitionStrategyKind
{
    None = 0,

    NumericWithUnit = 1,

    PoleCount = 2,

    EnumToken = 3,

    BooleanAlias = 4,

    Dimensions = 5,

    Dictionary = 6
}