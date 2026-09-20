using System.Globalization;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionMultiIntegerSamplePreparer
{
    public static CatalogRecognitionMultiIntegerSampleSet Prepare(
        Guid manufacturerId,
        Guid productTypeId,
        IReadOnlyList<CatalogRecognitionTrainingSampleSet> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var issues = new List<CatalogRecognitionTrainingIssue>();
        var samples = new List<CatalogRecognitionMultiIntegerSample>();

        if (manufacturerId == Guid.Empty || productTypeId == Guid.Empty)
        {
            issues.Add(new CatalogRecognitionTrainingIssue("InvalidScope", "Укажите производителя и тип товара.", Array.Empty<Guid>()));
        }

        if (sources.Count < 2 || sources.Count > 16)
        {
            issues.Add(new CatalogRecognitionTrainingIssue("InvalidCharacteristicCount", "Для составного числового шаблона выберите от 2 до 16 характеристик.", Array.Empty<Guid>()));
        }

        if (issues.Count > 0)
        {
            return new CatalogRecognitionMultiIntegerSampleSet(manufacturerId, productTypeId, [], samples, issues);
        }

        var characteristicIds = new HashSet<Guid>();

        foreach (var source in sources)
        {
            if (source is null || source.Scope is null || source.Samples is null)
            {
                issues.Add(new CatalogRecognitionTrainingIssue("InvalidSource", "Не переданы данные одной из характеристик.", Array.Empty<Guid>()));
                continue;
            }

            if (source.Scope.ManufacturerId != manufacturerId || source.Scope.ProductTypeId != productTypeId)
            {
                issues.Add(new CatalogRecognitionTrainingIssue("ScopeMismatch", "Нельзя объединять примеры разных производителей или типов товара.", Array.Empty<Guid>()));
            }

            if (source.Scope.CharacteristicDefinitionId == Guid.Empty || !characteristicIds.Add(source.Scope.CharacteristicDefinitionId))
            {
                issues.Add(new CatalogRecognitionTrainingIssue("RepeatedCharacteristic", "Характеристика не указана или передана несколько раз.", Array.Empty<Guid>()));
            }

            if (source.Samples.Count > 1000)
            {
                issues.Add(new CatalogRecognitionTrainingIssue("TooManyExamples", "Для одной характеристики передано более 1000 подтверждений.", Array.Empty<Guid>()));
            }
        }

        var orderedCharacteristicIds = characteristicIds.OrderBy(id => id).ToArray();

        if (issues.Count > 0)
        {
            return new CatalogRecognitionMultiIntegerSampleSet(manufacturerId, productTypeId, orderedCharacteristicIds, samples, issues);
        }

        var repeatedIds = sources.SelectMany(source => source.Samples)
            .GroupBy(sample => sample.ExampleId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(id => id)
            .ToArray();

        if (repeatedIds.Length > 0)
        {
            issues.Add(new CatalogRecognitionTrainingIssue("RepeatedExampleId", "Одно подтверждение передано несколько раз, в том числе для разных характеристик.", repeatedIds));
        }

        var preparedSources = sources
            .OrderBy(source => source.Scope.CharacteristicDefinitionId)
            .Select(CatalogRecognitionTrainingSamplePreparer.Prepare)
            .ToArray();

        foreach (var prepared in preparedSources)
        {
            issues.AddRange(prepared.Issues);
        }

        if (issues.Count > 0)
        {
            return new CatalogRecognitionMultiIntegerSampleSet(manufacturerId, productTypeId, orderedCharacteristicIds, samples, issues);
        }

        var names = preparedSources.SelectMany(source => source.Samples)
            .Select(sample => sample.ProductName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var samplesByCharacteristic = preparedSources.ToDictionary(
            source => source.Scope.CharacteristicDefinitionId,
            source => source.Samples.ToDictionary(sample => sample.ProductName, StringComparer.Ordinal));

        foreach (var name in names)
        {
            var tokens = CatalogRecognitionNameTokenizer.Tokenize(name);
            var annotations = new List<CatalogRecognitionMultiIntegerAnnotation>();

            foreach (var characteristicId in orderedCharacteristicIds)
            {
                if (!samplesByCharacteristic[characteristicId].TryGetValue(name, out var sample))
                {
                    issues.Add(new CatalogRecognitionTrainingIssue(
                        "IncompleteNameAnnotation",
                        $"Для названия «{name}» отсутствует подтверждение характеристики {characteristicId}.",
                        Array.Empty<Guid>()));
                    continue;
                }

                var annotation = PrepareAnnotation(characteristicId, sample, tokens);

                if (annotation is null)
                {
                    issues.Add(new CatalogRecognitionTrainingIssue(
                        "UnsupportedIntegerAnnotation",
                        $"Для названия «{name}» характеристика {characteristicId} должна быть размечена одним целым числом без единицы измерения.",
                        sample.ExampleIds));
                    continue;
                }

                annotations.Add(annotation);
            }

            if (annotations.Count != orderedCharacteristicIds.Length)
            {
                continue;
            }

            var overlappingIds = annotations
                .GroupBy(annotation => annotation.TokenIndex)
                .Where(group => group.Count() > 1)
                .SelectMany(group => group.SelectMany(annotation => annotation.ExampleIds))
                .Distinct()
                .OrderBy(id => id)
                .ToArray();

            if (overlappingIds.Length > 0)
            {
                issues.Add(new CatalogRecognitionTrainingIssue(
                    "OverlappingAnnotations",
                    $"В названии «{name}» один числовой фрагмент назначен нескольким характеристикам.",
                    overlappingIds));
                continue;
            }

            samples.Add(new CatalogRecognitionMultiIntegerSample(
                name,
                tokens,
                annotations.OrderBy(annotation => annotation.TokenIndex).ToArray()));
        }

        if (samples.Count < 2)
        {
            issues.Add(new CatalogRecognitionTrainingIssue(
                "InsufficientCompleteNames",
                "Нужны минимум два разных названия с подтверждёнными фрагментами всех выбранных характеристик.",
                Array.Empty<Guid>()));
        }

        return new CatalogRecognitionMultiIntegerSampleSet(manufacturerId, productTypeId, orderedCharacteristicIds, samples, issues);
    }

    private static CatalogRecognitionMultiIntegerAnnotation? PrepareAnnotation(
        Guid characteristicId,
        CatalogRecognitionPreparedSample sample,
        IReadOnlyList<CatalogRecognitionNameToken> tokens)
    {
        var span = CatalogRecognitionTokenSpanResolver.Resolve(tokens, sample.SpanStart, sample.SpanLength);

        if (span is null || span.TokenCount != 1)
        {
            return null;
        }

        var token = tokens[span.FirstTokenIndex];

        if (token.Kind != CatalogRecognitionNameTokenKind.Digits)
        {
            return null;
        }

        var normalizedValue = CatalogRecognitionIntegerPatternMatcher.NormalizeInteger(token.Text);

        if (normalizedValue is null || !string.Equals(normalizedValue, sample.NormalizedValue, StringComparison.Ordinal))
        {
            return null;
        }

        if (!decimal.TryParse(normalizedValue, NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            return null;
        }

        return new CatalogRecognitionMultiIntegerAnnotation(
            characteristicId,
            sample.RawValue,
            normalizedValue,
            sample.SpanStart,
            sample.SpanLength,
            span.FirstTokenIndex,
            sample.ExampleIds);
    }
}