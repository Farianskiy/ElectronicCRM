namespace ElectronicService.Domain.Users.Enums;

public enum UserPermissionCode
{
    None = 0,
    AssistantUse = 1,
    ProductsView = 2,
    PriceCalculationsManage = 3,
    PriceCalculationsExport = 4,
    CatalogImportsCreate = 5,
    CatalogImportsReview = 6,
    ProductsEdit = 7,
    PricesManage = 8,
    StockManage = 9,
    PriceListsManage = 10,
    DictionariesManage = 11,
    UsersManage = 12
}