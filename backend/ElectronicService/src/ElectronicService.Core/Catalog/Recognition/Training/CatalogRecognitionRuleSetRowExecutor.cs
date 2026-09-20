using System.Globalization;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionRuleSetRowExecutor
{
    public static Result<CatalogRecognitionRuleSetValueResolution, DomainError> Execute(
        CatalogRecognitionRuleSetExecutionSnapshot snapshot,
        Guid manufacturerId,
        Guid productTypeId,
        string productName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(productName);

        if (manufacturerId == Guid.Empty || productTypeId == Guid.Empty)
        {
            return new DomainError("training.invalid_request", "Укажите производителя и тип товара строки.");
        }

        if (snapshot.ManufacturerId != manufacturerId || snapshot.ProductTypeId != productTypeId)
        {
            return new DomainError("training.scope_mismatch", "Строка не относится к области действия выбранной версии.");
        }

        if (string.IsNullOrWhiteSpace(productName) || productName.Length > 2000)
        {
            return new DomainError("training.invalid_request", "Название должно содержать от 1 до 2000 символов.");
        }

        if (!IsValidSnapshot(snapshot))
        {
            return new DomainError(
                "training.invalid_data",
                "Версия содержит некорректные шаблоны или несовместимые версии генераторов.");
        }

        var candidates = new List<CatalogRecognitionRuleValueCandidate>();

        foreach (var rule in snapshot.LiteralRules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var positions = CatalogRecognitionLiteralMatcher.FindMatches(productName, rule.Literal);

            foreach (var position in positions)
            {
                candidates.Add(new CatalogRecognitionRuleValueCandidate(
                    CatalogRecognitionRuleKind.Literal,
                    rule.DraftId,
                    rule.CharacteristicDefinitionId,
                    rule.NormalizedValue,
                    productName.Substring(position, rule.Literal.Length),
                    position,
                    rule.Literal.Length));
            }

            if (candidates.Count > 10000)
            {
                return TooManyResults();
            }
        }

        foreach (var rule in snapshot.NumericRules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var captures = CatalogRecognitionIntegerAlternativesMatcher.Match(productName, rule.Pattern);

            foreach (var capture in captures)
            {
                if (!decimal.TryParse(
                    capture.NormalizedValue,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out _))
                {
                    return new DomainError(
                        "training.invalid_data",
                        $"Шаблон {rule.DraftId} извлёк число за пределами поддерживаемого диапазона.");
                }

                candidates.Add(new CatalogRecognitionRuleValueCandidate(
                    CatalogRecognitionRuleKind.NumericCapture,
                    rule.DraftId,
                    rule.CharacteristicDefinitionId,
                    capture.NormalizedValue,
                    capture.RawValue,
                    capture.SpanStart,
                    capture.SpanLength));
            }

            if (candidates.Count > 10000)
            {
                return TooManyResults();
            }
        }

        foreach (var rule in snapshot.MultiNumericRules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var match = CatalogRecognitionMultiIntegerMatcher.Match(productName, rule.Pattern);

            if (string.Equals(match.Status, "NoMatch", StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.Equals(match.Status, "Matched", StringComparison.Ordinal))
            {
                return new DomainError(
                    "training.invalid_data",
                    $"Не удалось исполнить составной шаблон {rule.DraftId}. Причина: {match.Status}.");
            }

            foreach (var capture in match.Captures)
            {
                candidates.Add(new CatalogRecognitionRuleValueCandidate(
                    CatalogRecognitionRuleKind.MultipleNumericCaptures,
                    rule.DraftId,
                    capture.CharacteristicDefinitionId,
                    capture.NormalizedValue,
                    capture.RawValue,
                    capture.SpanStart,
                    capture.SpanLength));
            }

            if (candidates.Count > 10000)
            {
                return TooManyResults();
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        return CatalogRecognitionRuleSetValueResolver.Resolve(productName, candidates);
    }

    private static bool IsValidSnapshot(CatalogRecognitionRuleSetExecutionSnapshot snapshot)
    {
        if (snapshot.VersionId == Guid.Empty ||
            snapshot.VersionNumber < 1 ||
            snapshot.LiteralRules is null ||
            snapshot.NumericRules is null ||
            snapshot.MultiNumericRules is null)
        {
            return false;
        }

        var count = (long)snapshot.LiteralRules.Count +
            snapshot.NumericRules.Count +
            snapshot.MultiNumericRules.Count;

        if (count < 1 || count > 100)
        {
            return false;
        }

        if (snapshot.LiteralRules.Any(rule => !IsValidLiteralRule(rule)) ||
            snapshot.NumericRules.Any(rule => !IsValidNumericRule(rule)) ||
            snapshot.MultiNumericRules.Any(rule => !IsValidMultiNumericRule(rule)))
        {
            return false;
        }

        return snapshot.LiteralRules.Select(rule => rule.DraftId).Distinct().Count() == snapshot.LiteralRules.Count &&
            snapshot.NumericRules.Select(rule => rule.DraftId).Distinct().Count() == snapshot.NumericRules.Count &&
            snapshot.MultiNumericRules.Select(rule => rule.DraftId).Distinct().Count() == snapshot.MultiNumericRules.Count;
    }

    private static bool IsValidLiteralRule(CatalogRecognitionLiteralExecutionRule? rule)
    {
        return rule is not null &&
            rule.DraftId != Guid.Empty &&
            rule.CharacteristicDefinitionId != Guid.Empty &&
            string.Equals(rule.GeneratorVersion, CatalogRecognitionLiteralProposalGenerator.Version, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(rule.Literal) &&
            rule.Literal.Length <= 2000 &&
            !string.IsNullOrWhiteSpace(rule.NormalizedValue) &&
            rule.NormalizedValue.Length <= 2000;
    }

    private static bool IsValidNumericRule(CatalogRecognitionNumericExecutionRule? rule)
    {
        if (rule is null ||
            rule.DraftId == Guid.Empty ||
            rule.CharacteristicDefinitionId == Guid.Empty ||
            !string.Equals(rule.GeneratorVersion, CatalogRecognitionIntegerAlternativesGenerator.Version, StringComparison.Ordinal))
        {
            return false;
        }

        var pattern = rule.Pattern;

        if (pattern is null ||
            pattern.Prefix is null ||
            pattern.Prefix.Length > 2000 ||
            pattern.Suffixes is null ||
            pattern.Suffixes.Count == 0 ||
            pattern.Suffixes.Count > 16)
        {
            return false;
        }

        return !pattern.Suffixes.Any(suffix =>
            suffix is null ||
            suffix.Length > 2000 ||
            (pattern.Prefix.Length == 0 && suffix.Length == 0));
    }

    private static bool IsValidMultiNumericRule(CatalogRecognitionMultiNumericExecutionRule? rule)
    {
        if (rule is null ||
            rule.DraftId == Guid.Empty ||
            !string.Equals(rule.GeneratorVersion, CatalogRecognitionMultiIntegerProposalGenerator.Version, StringComparison.Ordinal) ||
            !CatalogRecognitionMultiIntegerPatternValidator.IsValid(rule.Pattern))
        {
            return false;
        }

        return rule.Pattern.Parts.Count(part => part.CharacteristicDefinitionId.HasValue) >= 2;
    }

    private static DomainError TooManyResults()
    {
        return new DomainError(
            "training.selection_too_large",
            "Превышен лимит результатов правил для одной строки. Частичный результат не используется.");
    }
}