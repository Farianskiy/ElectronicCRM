using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Dictionaries;

public static class CatalogDictionaryTermErrors
{
    public static DomainError TermNotFound(Guid termId)
    {
        return new DomainError(
            "catalog.dictionary_term.not_found",
            $"Словарный термин '{termId}' не найден.");
    }

    public static DomainError StatusIsInvalid(CatalogDictionaryTermStatus status)
    {
        return new DomainError(
            "catalog.dictionary_term.status_invalid",
            $"Статус словарного термина '{status}' некорректен.");
    }

    public static DomainError InvalidStatusTransition(Guid termId, CatalogDictionaryTermStatus currentStatus, CatalogDictionaryTermStatus targetStatus)
    {
        return new DomainError(
            "catalog.dictionary_term.invalid_status_transition",
            $"Словарный термин '{termId}' нельзя перевести из статуса '{currentStatus}' в статус '{targetStatus}'.");
    }
}