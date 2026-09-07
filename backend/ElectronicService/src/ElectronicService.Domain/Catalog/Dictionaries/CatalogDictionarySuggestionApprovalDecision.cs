using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Dictionaries;

public sealed class CatalogDictionarySuggestionApprovalDecision
{
    public const int MinimumPriority = 1;

    public const int MaximumPriority = 10000;

    private CatalogDictionarySuggestionApprovalDecision(
        string phrase,
        CatalogDictionaryTermKind kind,
        string? targetCode,
        string targetValue,
        Guid? manufacturerId,
        Guid? productTypeId,
        Guid? characteristicDefinitionId,
        int priority)
    {
        Phrase = phrase;
        Kind = kind;
        TargetCode = targetCode;
        TargetValue = targetValue;
        ManufacturerId = manufacturerId;
        ProductTypeId = productTypeId;
        CharacteristicDefinitionId = characteristicDefinitionId;
        Priority = priority;
    }

    public string Phrase { get; }

    public CatalogDictionaryTermKind Kind { get; }

    public string? TargetCode { get; }

    public string TargetValue { get; }

    public Guid? ManufacturerId { get; }

    public Guid? ProductTypeId { get; }

    public Guid? CharacteristicDefinitionId { get; }

    public int Priority { get; }

    public static Result<CatalogDictionarySuggestionApprovalDecision, DomainError> Create(
        string phrase,
        CatalogDictionaryTermKind kind,
        string? targetCode,
        string targetValue,
        Guid? manufacturerId,
        Guid? productTypeId,
        Guid? characteristicDefinitionId,
        int priority)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return GeneralErrors.ValueIsInvalid(nameof(phrase));
        }

        if (kind == CatalogDictionaryTermKind.None || !Enum.IsDefined(kind))
        {
            return GeneralErrors.ValueIsInvalid(nameof(kind));
        }

        if (kind == CatalogDictionaryTermKind.Characteristic && string.IsNullOrWhiteSpace(targetCode))
        {
            return GeneralErrors.ValueIsInvalid(nameof(targetCode));
        }

        if (string.IsNullOrWhiteSpace(targetValue))
        {
            return GeneralErrors.ValueIsInvalid(nameof(targetValue));
        }

        if (manufacturerId.HasValue && manufacturerId.Value == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(manufacturerId));
        }

        if (productTypeId.HasValue && productTypeId.Value == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(productTypeId));
        }

        if (characteristicDefinitionId.HasValue && characteristicDefinitionId.Value == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(characteristicDefinitionId));
        }

        if (characteristicDefinitionId.HasValue && !productTypeId.HasValue)
        {
            return GeneralErrors.ValueIsInvalid(nameof(productTypeId));
        }

        if (characteristicDefinitionId.HasValue && kind != CatalogDictionaryTermKind.Characteristic)
        {
            return GeneralErrors.ValueIsInvalid(nameof(characteristicDefinitionId));
        }

        if (kind == CatalogDictionaryTermKind.Characteristic && productTypeId.HasValue && !characteristicDefinitionId.HasValue)
        {
            return GeneralErrors.ValueIsInvalid(nameof(characteristicDefinitionId));
        }

        if (priority is < MinimumPriority or > MaximumPriority)
        {
            return GeneralErrors.ValueIsInvalid(nameof(priority));
        }

        var normalizedPhrase = phrase.Trim();
        var normalizedTargetCode = NormalizeNullableText(targetCode);
        var normalizedTargetValue = NormalizeText(targetValue);

        if (normalizedPhrase.Length > CatalogAssistantDictionarySuggestion.UnknownPhraseMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(
                nameof(phrase),
                CatalogAssistantDictionarySuggestion.UnknownPhraseMaxLength);
        }

        if (normalizedTargetCode is not null && normalizedTargetCode.Length > CatalogAssistantDictionarySuggestion.SuggestedTargetCodeMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(
                nameof(targetCode),
                CatalogAssistantDictionarySuggestion.SuggestedTargetCodeMaxLength);
        }

        if (normalizedTargetValue.Length > CatalogAssistantDictionarySuggestion.SuggestedTargetValueMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(
                nameof(targetValue),
                CatalogAssistantDictionarySuggestion.SuggestedTargetValueMaxLength);
        }

        return new CatalogDictionarySuggestionApprovalDecision(
            normalizedPhrase,
            kind,
            normalizedTargetCode,
            normalizedTargetValue,
            manufacturerId,
            productTypeId,
            characteristicDefinitionId,
            priority);
    }

    private static string NormalizeText(string value)
    {
        return value.Trim().ToUpperInvariant().Replace("Ё", "Е", StringComparison.Ordinal);
    }

    private static string? NormalizeNullableText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NormalizeText(value);
    }
}