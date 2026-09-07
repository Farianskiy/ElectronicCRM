using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.PriceCalculations;

public static class CatalogPriceCalculationErrors
{
    public static DomainError CurrentUserNotFound()
    {
        return new DomainError(
            "catalog.price_calculation.current_user_not_found",
            "Не удалось определить пользователя, создающего расчёт цен.");
    }

    public static DomainError UserCannotCreateCalculation()
    {
        return new DomainError(
            "catalog.price_calculation.create_forbidden",
            "У пользователя нет прав на создание расчёта цен.");
    }

    public static DomainError CalculationNotFound(
        Guid calculationId)
    {
        return new DomainError(
            "catalog.price_calculation.not_found",
            $"Расчёт цен '{calculationId}' не найден.");
    }

    public static DomainError UserCannotModifyCalculation()
    {
        return new DomainError(
            "catalog.price_calculation.modify_forbidden",
            "Пользователь не может изменять этот расчёт цен.");
    }

    public static DomainError UserCannotViewCalculations()
    {
        return new DomainError(
            "catalog.price_calculation.view_forbidden",
            "У пользователя нет прав на просмотр расчётов цен.");
    }

    public static DomainError ActiveProductPriceNotFound(
        Guid productId)
    {
        return new DomainError(
            "catalog.price_calculation.active_price_not_found",
            $"Для товара '{productId}' не найдена корректная строка в активном прайс-листе производителя.");
    }

    public static DomainError ActiveProductPriceIsAmbiguous(
        Guid productId)
    {
        return new DomainError(
            "catalog.price_calculation.active_price_ambiguous",
            $"В активном прайс-листе найдено несколько строк товара '{productId}'. Необходимо исправить сопоставление прайса.");
    }

    public static DomainError CannotModifyCalculation(
        CatalogPriceCalculationStatus status)
    {
        return new DomainError(
            "catalog.price_calculation.cannot_modify",
            $"Расчёт цен в статусе '{status}' нельзя изменять.");
    }

    public static DomainError InvalidStatusTransition(
        CatalogPriceCalculationStatus currentStatus,
        CatalogPriceCalculationStatus targetStatus)
    {
        return new DomainError(
            "catalog.price_calculation.invalid_status_transition",
            $"Нельзя перевести расчёт цен из статуса '{currentStatus}' в статус '{targetStatus}'.");
    }

    public static DomainError CalculationMustContainLines()
    {
        return new DomainError(
            "catalog.price_calculation.lines_required",
            "Для завершения расчёта необходимо добавить хотя бы одну позицию.");
    }

    public static DomainError LineNotFound(
        Guid lineId)
    {
        return new DomainError(
            "catalog.price_calculation.line.not_found",
            $"Строка расчёта '{lineId}' не найдена.");
    }

    public static DomainError DuplicatePriceListRow(
        Guid priceListRowId)
    {
        return new DomainError(
            "catalog.price_calculation.line.duplicate_price_list_row",
            $"Строка прайс-листа '{priceListRowId}' уже добавлена в расчёт.");
    }

    public static DomainError QuantityMustBePositive()
    {
        return new DomainError(
            "catalog.price_calculation.line.quantity_not_positive",
            "Количество товара должно быть больше нуля.");
    }

    public static DomainError QuantityIsTooLarge(
        decimal maximumQuantity)
    {
        return new DomainError(
            "catalog.price_calculation.line.quantity_too_large",
            $"Количество товара не может превышать '{maximumQuantity}'.");
    }

    public static DomainError PriceCannotBeNegative(
        string propertyName)
    {
        return new DomainError(
            "catalog.price_calculation.line.price_negative",
            $"Цена '{propertyName}' не может быть отрицательной.");
    }

    public static DomainError PriceIsTooLarge(
        string propertyName,
        decimal maximumPrice)
    {
        return new DomainError(
            "catalog.price_calculation.line.price_too_large",
            $"Цена '{propertyName}' не может превышать '{maximumPrice}'.");
    }

    public static DomainError DiscountIsOutOfRange()
    {
        return new DomainError(
            "catalog.price_calculation.discount.out_of_range",
            "Скидка производителя должна находиться в диапазоне от 0 до 100 процентов.");
    }

    public static DomainError ManufacturerHasNoLines(
        Guid manufacturerId)
    {
        return new DomainError(
            "catalog.price_calculation.discount.manufacturer_has_no_lines",
            $"В расчёте отсутствуют позиции производителя '{manufacturerId}'.");
    }

    public static DomainError DiscountNotFound(
        Guid manufacturerId)
    {
        return new DomainError(
            "catalog.price_calculation.discount.not_found",
            $"Скидка производителя '{manufacturerId}' не найдена.");
    }
}