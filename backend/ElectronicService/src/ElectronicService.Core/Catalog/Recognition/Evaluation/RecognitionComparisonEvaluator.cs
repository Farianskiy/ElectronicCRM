using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Effective;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Core.Catalog.Recognition.Normalization;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Evaluation;

public sealed class RecognitionComparisonEvaluator(ICatalogEffectiveRecognitionService recognition)
{
    public const string Version = "effective-comparison-v1";
    public const int FormatVersion = 1;

    public async Task<Result<EvaluationResult, DomainError>> EvaluateAsync(EvaluationInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        var definitions = new List<CharacteristicDefinition>();
        foreach (var d in input.Definitions)
        {
            var restored = CharacteristicDefinition.RestoreSnapshot(d.Id, d.Code, d.Name, d.DataType, d.Unit);
            if (restored.IsFailure) return restored.Error;
            definitions.Add(restored.Value);
        }
        var byId = definitions.ToDictionary(x => x.Id);
        var current = CatalogRecognitionRunContext.FromSnapshot(input.CurrentState, input.CurrentRules, input.Terms, input.Profiles);
        var candidate = CatalogRecognitionRunContext.FromSnapshot(input.CurrentState with { ActiveVersionId = input.CandidateRules?.VersionId }, input.CandidateRules, input.CandidateTerms ?? input.Terms, input.Profiles);
        var overlap = input.EvidenceNames.ToHashSet(StringComparer.Ordinal);
        var rows = new List<EvaluationRow>();
        var duplicateCount = 0;
        foreach (var group in input.Examples.GroupBy(x => new { Name = CatalogRecognitionTextNormalizer.NormalizeText(x.ProductName), x.CharacteristicId })
            .OrderBy(x => x.Key.Name, StringComparer.Ordinal).ThenBy(x => x.Key.CharacteristicId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!byId.TryGetValue(group.Key.CharacteristicId, out var definition))
                return new DomainError("evaluation.invalid_label", "Характеристика подтверждения отсутствует в схеме области.");
            var values = new HashSet<string>(StringComparer.Ordinal);
            foreach (var example in group)
            {
                if (!CatalogRecognitionCharacteristicValueNormalizer.TryNormalizeRecognizedValue(definition, example.Value, input.ManufacturerName, out var normalized))
                    return new DomainError("evaluation.invalid_label", "Подтверждение содержит недопустимое значение характеристики.");
                values.Add(normalized);
            }
            duplicateCount += group.Count() - values.Count;
            var name = group.OrderBy(x => x.Id).First().ProductName;
            var before = await recognition.RecognizeAsync(new(name, input.ManufacturerId, input.ManufacturerName, input.ProductTypeId, definitions, current), cancellationToken).ConfigureAwait(false);
            if (before.IsFailure) return before.Error;
            var after = await recognition.RecognizeAsync(new(name, input.ManufacturerId, input.ManufacturerName, input.ProductTypeId, definitions, candidate), cancellationToken).ConfigureAwait(false);
            if (after.IsFailure) return after.Error;
            var expected = values.Order(StringComparer.Ordinal).ToArray();
            var first = Classify(before.Value.Recognition, definition, input.ManufacturerName, expected);
            var second = Classify(after.Value.Recognition, definition, input.ManufacturerName, expected);
            var change = Change(first, second);
            rows.Add(new(name, group.Key.Name, definition.Id, definition.Code, expected,
                group.OrderBy(x => x.Id).Select(x => new EvaluationSource(x.Id, x.FeedbackId)).ToArray(),
                overlap.Contains(group.Key.Name), values.Count != 1, first, second, change));
        }
        var controlRows = rows.Where(x => !x.TrainingOverlap && !x.LabelConflict).ToArray();
        var control = Metrics(controlRows);
        var reasons = new List<EvaluationReason>();
        if (control.Names < input.Policy.MinimumControlNames || control.Current.Total < input.Policy.MinimumControlUnits)
            reasons.Add(new("evaluation.insufficient_control", "Недостаточно контрольных примеров без пересечения с основаниями правил. Подтвердите новые имена и назначьте их только для контроля."));
        if (rows.Any(x => x.LabelConflict)) reasons.Add(new("evaluation.label_conflict", "Есть противоречивые подтверждения одной характеристики имени."));
        if (control.Regressions > 0) reasons.Add(new("evaluation.regression", "На контроле есть ранее правильные значения, которые новая версия ухудшает."));
        if (control.Improvements < input.Policy.MinimumImprovements) reasons.Add(new("evaluation.no_improvement", "Недостаточно исправленных контрольных значений."));
        if (control.Candidate.Incorrect > control.Current.Incorrect) reasons.Add(new("evaluation.more_errors", "Новая версия увеличивает число неправильных значений."));
        if (control.Candidate.Conflicts > control.Current.Conflicts) reasons.Add(new("evaluation.more_conflicts", "Новая версия увеличивает число конфликтов."));
        var state = reasons.Count == 0 ? "Ready" : "Failed";
        if (reasons.Any(x => string.Equals(x.Code, "evaluation.insufficient_control", StringComparison.Ordinal))) state = "InsufficientData";
        return new EvaluationResult(input.Examples.Count, duplicateCount, rows.Count(x => x.LabelConflict), rows.Count(x => x.TrainingOverlap),
            control, Metrics(rows.Where(x => x.TrainingOverlap && !x.LabelConflict).ToArray()),
            controlRows.GroupBy(x => x.CharacteristicId).OrderBy(x => x.Key).Select(x => new EvaluationCharacteristicMetrics(x.Key, Metrics(x.ToArray()))).ToArray(),
            rows.ToArray(), new(state, reasons));
    }

    private static EvaluationOutcome Classify(CatalogProductNameRecognitionResult result, CharacteristicDefinition definition, string manufacturer, string[] expected)
    {
        if (result.Conflicts.Any(x => string.Equals(x.CharacteristicCode, definition.Code, StringComparison.Ordinal))) return new("Conflict", null);
        var value = result.Characteristics.SingleOrDefault(x => string.Equals(x.CharacteristicCode, definition.Code, StringComparison.Ordinal));
        if (value is null) return new("Missing", null);
        if (!CatalogRecognitionCharacteristicValueNormalizer.TryNormalizeRecognizedValue(definition, value.NormalizedValue, manufacturer, out var normalized)) return new("Incorrect", value.NormalizedValue);
        return new(expected.Length == 1 && string.Equals(expected[0], normalized, StringComparison.Ordinal) ? "Correct" : "Incorrect", normalized);
    }

    private static string Change(EvaluationOutcome before, EvaluationOutcome after)
    {
        var wasCorrect = string.Equals(before.Status, "Correct", StringComparison.Ordinal);
        var nowCorrect = string.Equals(after.Status, "Correct", StringComparison.Ordinal);
        if (!wasCorrect && nowCorrect) return "Improved";
        if (wasCorrect && !nowCorrect) return "Regressed";
        return before == after ? "Unchanged" : "Changed";
    }

    private static EvaluationMetrics Metrics(EvaluationRow[] rows) => new(rows.Select(x => x.NormalizedName).Distinct(StringComparer.Ordinal).Count(),
        Counts(rows.Select(x => x.Current).ToArray()), Counts(rows.Select(x => x.Candidate).ToArray()),
        rows.Count(x => string.Equals(x.Change, "Improved", StringComparison.Ordinal)), rows.Count(x => string.Equals(x.Change, "Regressed", StringComparison.Ordinal)));
    private static EvaluationCounts Counts(EvaluationOutcome[] values) => new(values.Length,
        values.Count(x => string.Equals(x.Status, "Correct", StringComparison.Ordinal)), values.Count(x => string.Equals(x.Status, "Incorrect", StringComparison.Ordinal)),
        values.Count(x => string.Equals(x.Status, "Missing", StringComparison.Ordinal)), values.Count(x => string.Equals(x.Status, "Conflict", StringComparison.Ordinal)));
}
