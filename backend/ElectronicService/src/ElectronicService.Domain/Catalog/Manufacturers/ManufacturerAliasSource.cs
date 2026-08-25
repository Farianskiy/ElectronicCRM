namespace ElectronicService.Domain.Catalog.Manufacturers;

public enum ManufacturerAliasSource
{
    None = 0,

    Seed = 1,

    Import = 2,

    TechnicalUser = 3,

    UserCorrection = 4,

    RecognitionLearning = 5
}