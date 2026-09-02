using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Recognition;

public static class CatalogRecognitionErrors
{
    public static DomainError StrategyKindIsInvalid(CatalogCharacteristicRecognitionStrategyKind strategyKind)
    {
        return new DomainError(
            "catalog.recognition.profile.strategy_kind_invalid",
            $"Вид стратегии распознавания '{strategyKind}' некорректен.");
    }

    public static DomainError PriorityIsOutOfRange(
        int priority,
        int minimumPriority,
        int maximumPriority)
    {
        return new DomainError(
            "catalog.recognition.profile.priority_out_of_range",
            $"Приоритет профиля распознавания '{priority}' должен находиться в диапазоне от {minimumPriority} до {maximumPriority}.");
    }

    public static DomainError MinimumConfidenceIsOutOfRange(decimal minimumConfidence)
    {
        return new DomainError(
            "catalog.recognition.profile.minimum_confidence_out_of_range",
            $"Минимальная уверенность профиля распознавания '{minimumConfidence}' должна быть больше 0 и не больше 1.");
    }

    public static DomainError ConfigurationJsonIsInvalid()
    {
        return new DomainError(
            "catalog.recognition.profile.configuration_json_invalid",
            "Конфигурация профиля распознавания должна содержать корректный JSON.");
    }

    public static DomainError ConfigurationJsonMustBeObject()
    {
        return new DomainError(
            "catalog.recognition.profile.configuration_json_must_be_object",
            "Корневым значением конфигурации профиля распознавания должен быть JSON-объект.");
    }

    public static DomainError ConfigurationDoesNotMatchStrategy(CatalogCharacteristicRecognitionStrategyKind strategyKind)
    {
        return new DomainError(
            "catalog.recognition.profile.configuration_does_not_match_strategy",
            $"ConfigurationJson не соответствует настройкам стратегии '{strategyKind}'.");
    }

    public static DomainError ProfileNotFound(Guid profileId)
    {
        return new DomainError(
            "catalog.recognition.profile.not_found",
            $"Профиль распознавания '{profileId}' не найден.");
    }

    public static DomainError ProfileAlreadyExists(
        Guid productTypeId,
        Guid characteristicDefinitionId,
        CatalogCharacteristicRecognitionStrategyKind strategyKind)
    {
        return new DomainError(
            "catalog.recognition.profile.already_exists",
            $"Профиль распознавания для типа товара '{productTypeId}', характеристики '{characteristicDefinitionId}' и стратегии '{strategyKind}' уже существует.");
    }

    public static DomainError UserCannotManageProfiles()
    {
        return new DomainError(
            "catalog.recognition.profile.user_cannot_manage",
            "Только активный технический пользователь может управлять профилями распознавания.");
    }
}