using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Errors;

public static class CatalogErrors
{
    public static DomainError CharacteristicIsNotAllowedForProductType(
        Guid characteristicDefinitionId,
        Guid productTypeId)
    {
        return new DomainError(
            "catalog.characteristic_is_not_allowed_for_product_type",
            $"Характеристика '{characteristicDefinitionId}' не разрешена для типа товара '{productTypeId}'.");
    }

    public static DomainError CharacteristicAlreadyExists(string code)
    {
        return new DomainError(
            "catalog.characteristic_already_exists",
            $"Характеристика с кодом '{code}' уже существует.");
    }

    public static DomainError CharacteristicAlreadyAddedToProductType(string code)
    {
        return new DomainError(
            "catalog.characteristic_already_added_to_product_type",
            $"Характеристика '{code}' уже добавлена к этому типу товара.");
    }

    public static DomainError ProductAlreadyHasCharacteristic(string code)
    {
        return new DomainError(
            "catalog.product_already_has_characteristic",
            $"У товара уже есть характеристика '{code}'.");
    }

    public static DomainError ProductDoesNotHaveCharacteristic(Guid characteristicDefinitionId)
    {
        return new DomainError(
            "catalog.product_does_not_have_characteristic",
            $"У товара нет характеристики '{characteristicDefinitionId}'.");
    }

    public static DomainError CharacteristicValueTypeMismatch(
        string code,
        string expectedType,
        string actualType)
    {
        return new DomainError(
            "catalog.characteristic_value_type_mismatch",
            $"Характеристика '{code}' ожидает тип '{expectedType}', но получила '{actualType}'.");
    }

    public static DomainError RequiredCharacteristicIsMissing(string code)
    {
        return new DomainError(
            "catalog.required_characteristic_is_missing",
            $"Обязательная характеристика '{code}' не заполнена.");
    }

    public static DomainError ProductAliasAlreadyExists(string alias)
    {
        return new DomainError(
            "catalog.product_alias_already_exists",
            $"Алиас '{alias}' уже добавлен к товару.");
    }

    public static DomainError ProductTypeCannotBeChangedAfterCharacteristicsAdded()
    {
        return new DomainError(
            "catalog.product_type_cannot_be_changed_after_characteristics_added",
            "Тип товара нельзя изменить после добавления характеристик.");
    }

    public static DomainError ProductNotFound(string productId)
    {
        return new DomainError(
            "catalog.product.not_found",
            $"Товар '{productId}' не найден.");
    }

    public static DomainError ProductTypeNotFound(string productTypeId)
    {
        return new DomainError(
            "catalog.product_type.not_found",
            $"Тип товара '{productTypeId}' не найден.");
    }

    public static DomainError CharacteristicDefinitionNotFound(string code)
    {
        return new DomainError(
            "catalog.characteristic_definition.not_found",
            $"Характеристика с кодом '{code}' не найдена.");
    }

    public static DomainError DictionaryTermAlreadyExists(string phrase)
    {
        return new DomainError(
            "catalog.dictionary_term.already_exists",
            $"Термин словаря '{phrase}' уже существует.");
    }

    public static DomainError DictionarySuggestionNotFound(string suggestionId)
    {
        return new DomainError(
            "catalog.dictionary_suggestion.not_found",
            $"Предложение словаря '{suggestionId}' не найдено.");
    }

    public static DomainError OnlyTechnicalUserCanReviewDictionarySuggestions()
    {
        return new DomainError(
            "catalog.dictionary_suggestion.technical_user_required",
            "Только технический пользователь может проверять предложения словаря.");
    }

    public static DomainError OnlyTechnicalUserCanManageDictionaryTerms()
    {
        return new DomainError(
            "catalog.dictionary_term.technical_user_required",
            "Только технический пользователь может управлять словарём каталога.");
    }

    public static DomainError DictionarySuggestionAlreadyExists(string unknownPhrase)
    {
        return new DomainError(
            "catalog.dictionary_suggestion.already_exists",
            $"Предложение для слова '{unknownPhrase}' уже существует.");
    }

    public static DomainError UserCannotCreateDictionarySuggestion()
    {
        return new DomainError(
            "catalog.dictionary_suggestion.user_cannot_create",
            "Пользователь не может создавать предложения словаря.");
    }

    public static DomainError UserCannotReviewDictionarySuggestion()
    {
        return new DomainError(
            "catalog.dictionary_suggestion.user_cannot_review",
            "Пользователь не может проверять предложения словаря.");
    }

    public static DomainError CurrentUserIsRequired()
    {
        return new DomainError(
            "catalog.current_user.required",
            "Не удалось определить текущего пользователя.");
    }
}