namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionTrainingSamplePreparer
{
    public static CatalogRecognitionPreparedSampleSet Prepare(CatalogRecognitionTrainingSampleSet source)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(source.Scope);
        ArgumentNullException.ThrowIfNull(source.Samples);

        var issues = new List<CatalogRecognitionTrainingIssue>();
        var prepared = new List<CatalogRecognitionPreparedSample>();

        if (source.Scope.ManufacturerId == Guid.Empty || source.Scope.ProductTypeId == Guid.Empty || source.Scope.CharacteristicDefinitionId == Guid.Empty)
        {
            issues.Add(new CatalogRecognitionTrainingIssue("InvalidScope", "Не указан производитель, тип товара или характеристика.", Array.Empty<Guid>()));
        }

        if (source.HasMore)
        {
            issues.Add(new CatalogRecognitionTrainingIssue("IncompleteSelection", "Выборка превышает лимит. Генерация по неполной выборке запрещена.", Array.Empty<Guid>()));
        }

        if (source.Samples.Count == 0)
        {
            issues.Add(new CatalogRecognitionTrainingIssue("NoExamples", "Нет действующих подтверждённых примеров.", Array.Empty<Guid>()));
        }

        var repeatedIds = source.Samples.GroupBy(sample => sample.ExampleId).Where(group => group.Count() > 1).Select(group => group.Key).OrderBy(id => id).ToArray();

        if (repeatedIds.Length > 0)
        {
            issues.Add(new CatalogRecognitionTrainingIssue("RepeatedExampleId", "Один идентификатор учебного примера встретился несколько раз.", repeatedIds));
        }

        if (issues.Count > 0)
        {
            return new CatalogRecognitionPreparedSampleSet(source.Scope, source.Samples.Count, 0, prepared, issues);
        }

        var validSamples = new List<CatalogRecognitionTrainingSample>();

        foreach (var sample in source.Samples.OrderBy(sample => sample.ExampleId))
        {
            var issue = ValidateSample(sample);

            if (issue is not null)
            {
                issues.Add(issue);
                continue;
            }

            validSamples.Add(sample);
        }

        var duplicateCount = 0;
        var nameGroups = validSamples.GroupBy(sample => sample.ProductName, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal);

        foreach (var group in nameGroups)
        {
            var samples = group.OrderBy(sample => sample.ExampleId).ToArray();
            var exampleIds = samples.Select(sample => sample.ExampleId).ToArray();
            var values = samples.Select(sample => sample.NormalizedValue).Distinct(StringComparer.Ordinal).ToArray();

            if (values.Length > 1)
            {
                issues.Add(new CatalogRecognitionTrainingIssue("ConflictingValues", "Для одного названия подтверждены разные значения характеристики.", exampleIds));
                continue;
            }

            var spans = samples.Select(sample => (sample.SpanStart, sample.SpanLength)).Distinct().ToArray();

            if (spans.Length > 1)
            {
                issues.Add(new CatalogRecognitionTrainingIssue("AmbiguousSpan", "Для одного названия подтверждены разные выделения. Проверьте, какой фрагмент должен использоваться.", exampleIds));
                continue;
            }

            var first = samples[0];

            prepared.Add(new CatalogRecognitionPreparedSample(
                first.ProductName,
                first.RawValue,
                first.NormalizedValue,
                first.SpanStart,
                first.SpanLength,
                exampleIds));

            duplicateCount += samples.Length - 1;
        }

        return new CatalogRecognitionPreparedSampleSet(source.Scope, source.Samples.Count, duplicateCount, prepared, issues);
    }

    private static CatalogRecognitionTrainingIssue? ValidateSample(CatalogRecognitionTrainingSample sample)
    {
        if (sample.ExampleId == Guid.Empty || sample.SourceFeedbackId == Guid.Empty)
        {
            return CreateIssue(sample, "InvalidIdentifier", "У примера отсутствует идентификатор или ссылка на исходный Feedback.");
        }

        if (string.IsNullOrWhiteSpace(sample.ProductName) || string.IsNullOrWhiteSpace(sample.RawValue) || string.IsNullOrWhiteSpace(sample.NormalizedValue))
        {
            return CreateIssue(sample, "EmptyValue", "Название, выделенный фрагмент и правильное значение должны быть заполнены.");
        }

        if (sample.ProductName.Length > 2000 || sample.RawValue.Length > 2000 || sample.NormalizedValue.Length > 2000)
        {
            return CreateIssue(sample, "ValueTooLong", "Длина значения превышает допустимый размер учебного примера.");
        }

        if (sample.SpanStart < 0 || sample.SpanStart >= sample.ProductName.Length || sample.SpanLength <= 0 || sample.SpanLength > sample.ProductName.Length - sample.SpanStart)
        {
            return CreateIssue(sample, "InvalidSpan", "Выделение выходит за границы названия.");
        }

        var end = sample.SpanStart + sample.SpanLength;

        if (SplitsSurrogatePair(sample.ProductName, sample.SpanStart) || SplitsSurrogatePair(sample.ProductName, end))
        {
            return CreateIssue(sample, "InvalidCharacterBoundary", "Граница выделения разрезает символ.");
        }

        var actualFragment = sample.ProductName.Substring(sample.SpanStart, sample.SpanLength);

        if (!string.Equals(actualFragment, sample.RawValue, StringComparison.Ordinal))
        {
            return CreateIssue(sample, "FragmentMismatch", "Сохранённый фрагмент не совпадает с выделенным участком названия.");
        }

        return null;
    }

    private static bool SplitsSurrogatePair(string value, int position)
    {
        return position > 0 && position < value.Length && char.IsHighSurrogate(value[position - 1]) && char.IsLowSurrogate(value[position]);
    }

    private static CatalogRecognitionTrainingIssue CreateIssue(CatalogRecognitionTrainingSample sample, string code, string message)
    {
        return new CatalogRecognitionTrainingIssue(code, message, new[] { sample.ExampleId });
    }
}