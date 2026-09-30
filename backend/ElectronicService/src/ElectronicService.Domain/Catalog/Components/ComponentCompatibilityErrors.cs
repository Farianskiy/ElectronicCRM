using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Components;

public static class ComponentCompatibilityErrors
{
    public static DomainError NeedNotFound(Guid needId) => new(
        "catalog.component_need.not_found",
        $"Потребность '{needId}' не найдена.");

    public static DomainError NeedAlreadyExists(string code) => new(
        "catalog.component_need.already_exists",
        $"Потребность с кодом '{code}' уже существует для этого типа товара.");

    public static DomainError MainProductTypeRequired() => new(
        "catalog.component_need.main_product_type_required",
        "Потребность можно создать только для основного типа товара.");

    public static DomainError ComponentProductRequired() => new(
        "catalog.component_offer.component_product_required",
        "Предложение совместимости можно настроить только для комплектующего.");

    public static DomainError OfferAlreadyExists(
        Guid componentProductId,
        Guid needId) => new(
        "catalog.component_offer.already_exists",
        $"Комплектующее '{componentProductId}' уже привязано к потребности '{needId}'.");

    public static DomainError NeedDoesNotBelongToProductType() => new(
        "catalog.component_need.product_type_mismatch",
        "Потребность не относится к типу выбранного основного товара.");

    public static DomainError ConstraintAlreadyExists(Guid definitionId) => new(
        "catalog.component_constraint.already_exists",
        $"Такое значение совместимости характеристики '{definitionId}' уже добавлено.");

    public static DomainError ProductCannotSelectItself() => new(
        "catalog.selected_component.same_product",
        "Основной товар нельзя выбрать как собственное комплектующее.");

    public static DomainError InvalidSelectedQuantity(int maximumQuantity) => new(
        "catalog.selected_component.invalid_quantity",
        $"Количество комплектующего должно быть от 1 до {maximumQuantity}.");

    public static DomainError CompatibleOfferNotFound() => new(
        "catalog.selected_component.compatible_offer_not_found",
        "Для выбранного товара не найдено подходящее предложение комплектующего.");

    public static DomainError SelectedComponentAlreadyExists() => new(
        "catalog.selected_component.already_exists",
        "Это комплектующее уже выбрано для потребности основного товара.");

    public static DomainError SelectedComponentNotFound(Guid selectionId) => new(
        "catalog.selected_component.not_found",
        $"Выбранное комплектующее '{selectionId}' не найдено.");

    public static DomainError MainProductRequired() => new(
        "catalog.selected_component.main_product_required",
        "Комплектацию можно настраивать только для основного товара.");
}
