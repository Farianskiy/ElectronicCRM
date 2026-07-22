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

    public static DomainError CharacteristicDefinitionNotFound(
    Guid characteristicDefinitionId)
    {
        return new DomainError(
            "catalog.characteristic_definition.not_found",
            $"Характеристика " +
            $"'{characteristicDefinitionId}' не найдена.");
    }

    public static DomainError ProductTypeCharacteristicNotFound(
    Guid productTypeId,
    Guid characteristicDefinitionId)
    {
        return new DomainError(
            "catalog.product_type_characteristic.not_found",
            $"Характеристика '{characteristicDefinitionId}' " +
            $"не добавлена к типу товара '{productTypeId}'.");
    }

    public static DomainError CharacteristicCannotBeMadeRequired(
        Guid characteristicDefinitionId,
        int productsWithoutValueCount)
    {
        return new DomainError(
            "catalog.product_type_characteristic.cannot_be_required",
            $"Характеристику '{characteristicDefinitionId}' " +
            $"нельзя сделать обязательной: " +
            $"{productsWithoutValueCount} товаров " +
            $"не имеют значения.");
    }

    public static DomainError
    ProductTypeCharacteristicCannotBeRemoved(
        Guid productTypeId,
        Guid characteristicDefinitionId,
        int productsWithValueCount)
    {
        return new DomainError(
            "catalog.product_type_characteristic.cannot_be_removed",
            $"Характеристику '{characteristicDefinitionId}' " +
            $"нельзя удалить из типа товара " +
            $"'{productTypeId}': она заполнена у " +
            $"{productsWithValueCount} товаров.");
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

    public static DomainError ManufacturerNotFound(Guid manufacturerId)
    {
        return new DomainError(
            "catalog.manufacturer.not_found",
            $"Производитель '{manufacturerId}' не найден.");
    }

    public static DomainError ProductAliasNotFound(Guid aliasId)
    {
        return new DomainError(
            "catalog.product_alias.not_found",
            $"Альтернативное название '{aliasId}' не найдено.");
    }

    public static DomainError RequiredCharacteristicCannotBeRemoved(
        Guid characteristicDefinitionId)
    {
        return new DomainError(
            "catalog.required_characteristic.cannot_be_removed",
            $"Обязательную характеристику " +
            $"'{characteristicDefinitionId}' удалить нельзя.");
    }

    public static DomainError
    ProductTypeMigrationTargetMustBeDifferent(
        Guid productTypeId)
    {
        return new DomainError(
            "catalog.product_type_migration.target_must_be_different",
            $"Товар уже относится к типу " +
            $"'{productTypeId}'. Выберите другой тип.");
    }

    public static DomainError
    ProductTypeMigrationPreviewIsStale()
    {
        return new DomainError(
            "catalog.product_type_migration.preview_is_stale",
            "Состояние товара или схема типа изменились " +
            "после предварительного просмотра. " +
            "Постройте preview заново.");
    }

    public static DomainError
        ProductTypeMigrationDuplicateValue(
            Guid characteristicDefinitionId)
    {
        return new DomainError(
            "catalog.product_type_migration.duplicate_value",
            $"Значение характеристики " +
            $"'{characteristicDefinitionId}' " +
            $"передано несколько раз.");
    }

    public static DomainError
        ProductTypeMigrationRequiredValueMissing(
            Guid characteristicDefinitionId)
    {
        return new DomainError(
            "catalog.product_type_migration.required_value_missing",
            $"Не передано обязательное значение " +
            $"характеристики " +
            $"'{characteristicDefinitionId}'.");
    }

    public static DomainError
        ProductTypeMigrationUnexpectedValue(
            Guid characteristicDefinitionId)
    {
        return new DomainError(
            "catalog.product_type_migration.unexpected_value",
            $"Характеристика " +
            $"'{characteristicDefinitionId}' " +
            $"не ожидается в этой миграции.");
    }

    public static DomainError
        ProductTypeMigrationValueIsInvalid(
            string characteristicCode,
            string expectedDataType)
    {
        return new DomainError(
            "catalog.product_type_migration.value_is_invalid",
            $"Значение характеристики " +
            $"'{characteristicCode}' некорректно. " +
            $"Ожидаемый тип: {expectedDataType}.");
    }

    public static DomainError ProductConcurrencyConflict(
    Guid productId)
    {
        return new DomainError(
            "catalog.product.concurrency_conflict",
            $"Товар '{productId}' был изменён другим " +
            "пользователем или процессом. " +
            "Обновите данные и повторите операцию.");
    }
}