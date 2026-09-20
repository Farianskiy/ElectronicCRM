using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionRuleSetValueResolver
{
    public static Result<CatalogRecognitionRuleSetValueResolution, DomainError> Resolve(
        string productName,
        IReadOnlyList<CatalogRecognitionRuleValueCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(productName);
        ArgumentNullException.ThrowIfNull(candidates);

        if (string.IsNullOrWhiteSpace(productName) || productName.Length > 2000)
        {
            return new DomainError(
                "training.invalid_request",
                "Для проверки требуется название длиной от 1 до 2000 символов.");
        }

        if (candidates.Count > 10000)
        {
            return new DomainError(
                "training.selection_too_large",
                "Превышен лимит результатов правил для одной строки.");
        }

        if (candidates.Any(candidate => !IsValidCandidate(productName, candidate)))
        {
            return new DomainError(
                "training.invalid_data",
                "Одно из правил вернуло некорректное значение или фрагмент названия. Объединение результатов остановлено.");
        }

        var results = new List<CatalogRecognitionResolvedCharacteristic>();

        var groups = candidates
            .Distinct()
            .GroupBy(candidate => candidate.CharacteristicDefinitionId)
            .OrderBy(group => group.Key);

        foreach (var group in groups)
        {
            var sources = group
                .OrderBy(candidate => candidate.Kind)
                .ThenBy(candidate => candidate.DraftId)
                .ThenBy(candidate => candidate.SpanStart)
                .ThenBy(candidate => candidate.SpanLength)
                .ThenBy(candidate => candidate.NormalizedValue, StringComparer.Ordinal)
                .ToArray();

            var values = sources
                .Select(candidate => candidate.NormalizedValue)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            var hasConflict = values.Length > 1;
            string? proposedValue = null;

            if (!hasConflict)
            {
                proposedValue = values[0];
            }

            results.Add(new CatalogRecognitionResolvedCharacteristic(
                group.Key,
                proposedValue,
                hasConflict,
                values,
                sources));
        }

        return new CatalogRecognitionRuleSetValueResolution(results);
    }

    private static bool IsValidCandidate(
        string productName,
        CatalogRecognitionRuleValueCandidate? candidate)
    {
        if (candidate is null)
        {
            return false;
        }

        if (candidate.Kind == CatalogRecognitionRuleKind.None ||
            !Enum.IsDefined(candidate.Kind) ||
            candidate.DraftId == Guid.Empty ||
            candidate.CharacteristicDefinitionId == Guid.Empty)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(candidate.NormalizedValue) ||
            candidate.NormalizedValue.Length > 2000 ||
            string.IsNullOrEmpty(candidate.RawValue))
        {
            return false;
        }

        if (candidate.SpanStart < 0 ||
            candidate.SpanStart >= productName.Length ||
            candidate.SpanLength <= 0 ||
            candidate.SpanLength > productName.Length - candidate.SpanStart)
        {
            return false;
        }

        var actualFragment = productName.Substring(
            candidate.SpanStart,
            candidate.SpanLength);

        return string.Equals(
            actualFragment,
            candidate.RawValue,
            StringComparison.Ordinal);
    }
}