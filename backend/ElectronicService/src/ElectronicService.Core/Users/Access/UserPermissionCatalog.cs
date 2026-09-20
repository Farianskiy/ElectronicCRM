using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Users.Access;

public sealed record UserPermissionDefinition(
    UserPermissionCode Code,
    string Group,
    string Label);

public static class UserPermissionCatalog
{
    public static readonly IReadOnlyCollection<UserPermissionDefinition> Definitions =
    [
        new(UserPermissionCode.AssistantUse, "Работа", "Использование ассистента"),
        new(UserPermissionCode.ProductsView, "Работа", "Просмотр товаров"),
        new(UserPermissionCode.PriceCalculationsManage, "Работа", "Создание и изменение расчётов"),
        new(UserPermissionCode.PriceCalculationsExport, "Работа", "Выгрузка расчётов в Excel"),
        new(UserPermissionCode.CatalogImportsCreate, "Работа", "Создание импорта Excel"),
        new(UserPermissionCode.CatalogImportsReview, "Каталог и качество", "Проверка и применение импорта"),
        new(UserPermissionCode.ProductsEdit, "Каталог и качество", "Изменение товаров"),
        new(UserPermissionCode.PricesManage, "Каталог и качество", "Изменение цен"),
        new(UserPermissionCode.StockManage, "Каталог и качество", "Изменение остатков"),
        new(UserPermissionCode.PriceListsManage, "Каталог и качество", "Управление прайс-листами"),
        new(UserPermissionCode.DictionariesManage, "Каталог и качество", "Управление справочниками"),
        new(UserPermissionCode.UsersManage, "Администрирование", "Управление пользователями, ролями и правами")
    ];

    public static IReadOnlySet<UserPermissionCode> GetDefaults(UserType type)
    {
        return type switch
        {
            UserType.Regular => new HashSet<UserPermissionCode>
            {
                UserPermissionCode.AssistantUse,
                UserPermissionCode.ProductsView
            },

            UserType.Manager => new HashSet<UserPermissionCode>
            {
                UserPermissionCode.AssistantUse,
                UserPermissionCode.ProductsView,
                UserPermissionCode.PriceCalculationsManage,
                UserPermissionCode.PriceCalculationsExport,
                UserPermissionCode.CatalogImportsCreate
            },

            UserType.Technical => new HashSet<UserPermissionCode>
            {
                UserPermissionCode.ProductsView,
                UserPermissionCode.CatalogImportsCreate,
                UserPermissionCode.CatalogImportsReview,
                UserPermissionCode.ProductsEdit,
                UserPermissionCode.PricesManage,
                UserPermissionCode.StockManage,
                UserPermissionCode.PriceListsManage,
                UserPermissionCode.DictionariesManage
            },

            UserType.Administrator => new HashSet<UserPermissionCode>
            {
                UserPermissionCode.UsersManage
            },

            UserType.SystemDeveloper => Definitions
                .Select(value => value.Code)
                .ToHashSet(),

            _ => new HashSet<UserPermissionCode>()
        };
    }
}