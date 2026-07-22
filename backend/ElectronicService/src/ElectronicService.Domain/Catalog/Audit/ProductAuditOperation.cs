namespace ElectronicService.Domain.Catalog.Audit;

public enum ProductAuditOperation
{
    None = 0,

    GeneralInformationUpdated = 1,
    PriceUpdated = 2,
    StockUpdated = 3,

    CharacteristicSet = 4,
    CharacteristicRemoved = 5,

    AliasAdded = 6,
    AliasRemoved = 7,

    ProductTypeMigrated = 8,

    ImportApplied = 9
}