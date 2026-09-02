using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Core.Catalog.Recognition.Normalization;
using ElectronicService.Core.Catalog.ProductNames.Explanation;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Core.Catalog.Characteristics.Normalization;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed class CatalogImportRecognitionShadowService
    : ICatalogImportRecognitionShadowService
{
    private const int MaximumSamples = 100;

    private const int MaximumExamplesPerConflictGroup = 5;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly ICatalogProductNameRecognitionService _recognitionService;

    public CatalogImportRecognitionShadowService(
        ICatalogProductNameRecognitionService recognitionService)
    {
        ArgumentNullException.ThrowIfNull(recognitionService);

        _recognitionService = recognitionService;
    }

    public async Task<CatalogImportRecognitionShadowResult> AnalyzeAsync(
        CatalogImportWorkbookAnalysis analysis,
        ProductType productType,
        IReadOnlyCollection<CharacteristicDefinition> characteristicDefinitions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(productType);
        ArgumentNullException.ThrowIfNull(characteristicDefinitions);

        var definitions = characteristicDefinitions
            .Where(definition =>
                productType.AllowsCharacteristic(definition.Id))
            .ToArray();

        var definitionsByCode = definitions
            .GroupBy(
                definition =>
                    CatalogRecognitionTextNormalizer.NormalizeCode(
                        definition.Code),
                StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(
                group => group.Key,
                group => group.Single(),
                StringComparer.Ordinal);

        var allowedCharacteristicCodes = definitionsByCode
            .Keys
            .ToArray();

        var statistics = definitionsByCode
            .Values
            .ToDictionary(
                definition =>
                    CatalogRecognitionTextNormalizer.NormalizeCode(
                        definition.Code),
                _ => new MutableCharacteristicStatistics(),
                StringComparer.Ordinal);

        var samples =
            new List<CatalogImportRecognitionShadowSample>(
                MaximumSamples);

        var evidenceRows =
            new List<CatalogImportProductNameEvidenceRow>();

        var conflictGroups =
            new Dictionary<
                ConflictGroupKey,
                MutableConflictGroup>();

        var rowsAnalyzed = 0;
        var rowsWithRecognition = 0;
        var failedRowsCount = 0;

        foreach (var row in analysis.Rows.OrderBy(
                     row => row.RowNumber))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var data = DeserializeNormalizedData(row);

            if (data is null ||
                string.IsNullOrWhiteSpace(data.Name))
            {
                continue;
            }

            rowsAnalyzed++;

            CatalogProductNameRecognitionResult recognitionResult;

            try
            {
                recognitionResult = await _recognitionService
                    .RecognizeAsync(
                        new CatalogProductNameRecognitionRequest(
                            data.Name,
                            productType.Id,
                            allowedCharacteristicCodes),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (RegexMatchTimeoutException)
            {
                failedRowsCount++;

                continue;
            }

            var rowEvidence = recognitionResult.Candidates
                .Where(characteristic =>
                    definitionsByCode.ContainsKey(
                        CatalogRecognitionTextNormalizer.NormalizeCode(
                            characteristic.CharacteristicCode)))
                .Select(MapExplanationEvidence)
                .ToArray();

            if (rowEvidence.Length > 0)
            {
                evidenceRows.Add(
                    new CatalogImportProductNameEvidenceRow(
                        row.RowNumber,
                        rowEvidence));
            }

            var recognizedByCode = recognitionResult
                .Characteristics
                .Where(characteristic =>
                    definitionsByCode.ContainsKey(
                        CatalogRecognitionTextNormalizer.NormalizeCode(
                            characteristic.CharacteristicCode)))
                .GroupBy(
                    characteristic =>
                        CatalogRecognitionTextNormalizer.NormalizeCode(
                            characteristic.CharacteristicCode),
                    StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(characteristic =>
                            characteristic.Confidence)
                        .ThenByDescending(characteristic =>
                            characteristic.Priority)
                        .First(),
                    StringComparer.Ordinal);

            var conflictsByCode = recognitionResult
                .Conflicts
                .Where(conflict =>
                    definitionsByCode.ContainsKey(
                        CatalogRecognitionTextNormalizer.NormalizeCode(
                            conflict.CharacteristicCode)))
                .GroupBy(
                    conflict =>
                        CatalogRecognitionTextNormalizer.NormalizeCode(
                            conflict.CharacteristicCode),
                    StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.Ordinal);

            if (recognizedByCode.Count > 0 ||
                conflictsByCode.Count > 0)
            {
                rowsWithRecognition++;
            }

            foreach (var recognizedItem in recognizedByCode)
            {
                statistics[
                    recognizedItem.Key]
                    .RecognizedValuesCount++;
            }

            foreach (var conflictItem in conflictsByCode)
            {
                statistics[
                    conflictItem.Key]
                    .AmbiguousCount++;
            }

            foreach (var definitionItem in definitionsByCode)
            {
                var characteristicCode = definitionItem.Key;
                var definition = definitionItem.Value;

                var characteristicStatistics =
                    statistics[characteristicCode];

                var characteristicId =
                    definition.Id.ToString();

                var hasExplicitValue =
                    data.Characteristics.TryGetValue(
                        characteristicId,
                        out var explicitValue) &&
                    !string.IsNullOrWhiteSpace(
                        explicitValue);

                if (hasExplicitValue)
                {
                    characteristicStatistics
                        .ExplicitValuesCount++;
                }

                if (conflictsByCode.TryGetValue(
                        characteristicCode,
                        out var recognitionConflict))
                {
                    if (samples.Count < MaximumSamples)
                    {
                        var candidateValues =
                            recognitionConflict.Candidates
                                .Select(candidate =>
                                    candidate.NormalizedValue)
                                .Distinct(StringComparer.Ordinal)
                                .ToArray();

                        var candidateRawValues =
                            recognitionConflict.Candidates
                                .Select(candidate =>
                                    candidate.RawValue)
                                .Distinct(StringComparer.Ordinal)
                                .ToArray();

                        var candidateSources =
                            recognitionConflict.Candidates
                                .Select(candidate =>
                                    candidate.Source.ToString())
                                .Distinct(StringComparer.Ordinal)
                                .ToArray();

                        var candidateRecognizerKeys =
                            recognitionConflict.Candidates
                                .Select(candidate =>
                                    candidate.RecognizerKey)
                                .Distinct(StringComparer.Ordinal)
                                .ToArray();

                        samples.Add(
                            new CatalogImportRecognitionShadowSample(
                                row.RowNumber,
                                CatalogImportRecognitionShadowSampleKind
                                    .Ambiguous,
                                characteristicCode,
                                definition.Name,
                                data.Name,
                                hasExplicitValue
                                    ? explicitValue
                                    : null,
                                string.Join(
                                    " | ",
                                    candidateValues),
                                string.Join(
                                    " | ",
                                    candidateRawValues),
                                recognitionConflict.Candidates.Max(
                                    candidate =>
                                        candidate.Confidence),
                                string.Join(
                                    " | ",
                                    candidateSources),
                                string.Join(
                                    " | ",
                                    candidateRecognizerKeys),
                                null,
                                null,
                                recognitionConflict.Candidates.Max(
                                    candidate =>
                                        candidate.Priority),
                                "Recognition Engine нашёл несколько разных значений одной характеристики от источников одинакового уровня надёжности."));
                    }

                    continue;
                }

                if (!recognizedByCode.TryGetValue(
                        characteristicCode,
                        out var recognizedCharacteristic))
                {
                    if (hasExplicitValue)
                    {
                        characteristicStatistics
                            .NotRecognizedCount++;

                        if (samples.Count < MaximumSamples)
                        {
                            samples.Add(
                                new CatalogImportRecognitionShadowSample(
                                    row.RowNumber,
                                    CatalogImportRecognitionShadowSampleKind
                                        .NotRecognized,
                                    characteristicCode,
                                    definition.Name,
                                    data.Name,
                                    explicitValue,
                                    null,
                                    null,
                                    null,
                                    null,
                                    null,
                                    null,
                                    null,
                                    null,
                                    "В Excel значение присутствует, но из наименования характеристика не распознана."));
                        }
                    }

                    continue;
                }

                if (!hasExplicitValue)
                {
                    characteristicStatistics
                        .RecognitionWithoutExplicitValueCount++;

                    if (samples.Count < MaximumSamples)
                    {
                        samples.Add(
                            new CatalogImportRecognitionShadowSample(
                                row.RowNumber,
                                CatalogImportRecognitionShadowSampleKind
                                    .RecognitionWithoutExplicitValue,
                                characteristicCode,
                                definition.Name,
                                data.Name,
                                null,
                                recognizedCharacteristic.NormalizedValue,
                                recognizedCharacteristic.RawValue,
                                recognizedCharacteristic.Confidence,
                                recognizedCharacteristic.Source.ToString(),
                                recognizedCharacteristic.RecognizerKey,
                                recognizedCharacteristic.StartIndex,
                                recognizedCharacteristic.Length,
                                recognizedCharacteristic.Priority,
                                "Характеристика найдена в наименовании, но явного значения Excel нет. В Shadow Mode значение не применяется."));
                    }

                    continue;
                }

                if (AreEquivalent(
                        definition,
                        explicitValue!,
                        recognizedCharacteristic.NormalizedValue))
                {
                    characteristicStatistics.MatchesCount++;

                    continue;
                }

                characteristicStatistics.ConflictsCount++;

                AddConflictGroup(
                    conflictGroups,
                    row.RowNumber,
                    definition,
                    data.Name,
                    explicitValue!,
                    recognizedCharacteristic);

                if (samples.Count < MaximumSamples)
                {
                    samples.Add(
                        new CatalogImportRecognitionShadowSample(
                            row.RowNumber,
                            CatalogImportRecognitionShadowSampleKind
                                .Conflict,
                            characteristicCode,
                            definition.Name,
                            data.Name,
                            explicitValue,
                            recognizedCharacteristic.NormalizedValue,
                            recognizedCharacteristic.RawValue,
                            recognizedCharacteristic.Confidence,
                            recognizedCharacteristic.Source.ToString(),
                            recognizedCharacteristic.RecognizerKey,
                            recognizedCharacteristic.StartIndex,
                            recognizedCharacteristic.Length,
                            recognizedCharacteristic.Priority,
                            $"Значение Excel отличается от значения, найденного источником '{recognizedCharacteristic.Source}' в наименовании."));
                }
            }
        }

        var characteristicResults = definitionsByCode
            .OrderBy(
                item => item.Key,
                StringComparer.Ordinal)
            .Select(item =>
            {
                var currentStatistics =
                    statistics[item.Key];

                return new CatalogImportRecognitionShadowCharacteristicStatistics(
                    item.Key,
                    item.Value.Name,
                    currentStatistics.ExplicitValuesCount,
                    currentStatistics.RecognizedValuesCount,
                    currentStatistics.MatchesCount,
                    currentStatistics.ConflictsCount,
                    currentStatistics.NotRecognizedCount,
                    currentStatistics
                        .RecognitionWithoutExplicitValueCount,
                    currentStatistics.AmbiguousCount);
            })
            .ToArray();

        var conflictGroupResults = conflictGroups
            .Values
            .Select(group => group.ToResult())
            .OrderByDescending(group =>
                group.OccurrenceCount)
            .ThenBy(group =>
                group.CharacteristicCode,
                StringComparer.Ordinal)
            .ThenBy(group =>
                group.ExcelValue,
                StringComparer.Ordinal)
            .ThenBy(group =>
                group.RecognizedValue,
                StringComparer.Ordinal)
            .ToArray();

        return new CatalogImportRecognitionShadowResult(
            rowsAnalyzed,
            rowsWithRecognition,
            failedRowsCount,
            characteristicResults.Sum(item =>
                item.ExplicitValuesCount),
            characteristicResults.Sum(item =>
                item.RecognizedValuesCount),
            characteristicResults.Sum(item =>
                item.MatchesCount),
            characteristicResults.Sum(item =>
                item.ConflictsCount),
            characteristicResults.Sum(item =>
                item.NotRecognizedCount),
            characteristicResults.Sum(item =>
                item.RecognitionWithoutExplicitValueCount),
            characteristicResults.Sum(item =>
                item.AmbiguousCount),
            characteristicResults,
            conflictGroupResults,
            samples,
            evidenceRows);
    }

    private static CatalogProductNameEvidenceSpan MapExplanationEvidence(
        CatalogRecognizedCharacteristic characteristic)
    {
        return new CatalogProductNameEvidenceSpan(
            CatalogProductNameEvidenceKind.Characteristic,
            characteristic.CharacteristicCode,
            characteristic.NormalizedValue,
            characteristic.RawValue,
            characteristic.Source.ToString(),
            characteristic.Confidence,
            characteristic.Priority,
            characteristic.StartIndex,
            characteristic.Length);
    }


    private static void AddConflictGroup(
        Dictionary<ConflictGroupKey, MutableConflictGroup> conflictGroups,
        int rowNumber,
        CharacteristicDefinition definition,
        string productName,
        string excelValue,
        CatalogRecognizedCharacteristic recognizedCharacteristic)
    {
        var normalizedCharacteristicCode =
            CatalogRecognitionTextNormalizer.NormalizeCode(
                definition.Code);

        var normalizedExcelValue =
            NormalizeComparableValue(
                definition,
                excelValue);

        var normalizedRecognizedValue =
            NormalizeComparableValue(
                definition,
                recognizedCharacteristic.NormalizedValue);

        var key = new ConflictGroupKey(
            normalizedCharacteristicCode,
            normalizedExcelValue,
            normalizedRecognizedValue,
            recognizedCharacteristic.Source,
            recognizedCharacteristic.RecognizerKey);

        if (!conflictGroups.TryGetValue(
                key,
                out var conflictGroup))
        {
            conflictGroup = new MutableConflictGroup(
                normalizedCharacteristicCode,
                definition.Name,
                excelValue,
                recognizedCharacteristic.NormalizedValue,
                recognizedCharacteristic.RawValue,
                recognizedCharacteristic.Source,
                recognizedCharacteristic.Confidence,
                recognizedCharacteristic.StartIndex,
                recognizedCharacteristic.Length,
                recognizedCharacteristic.Priority,
                recognizedCharacteristic.RecognizerKey);

            conflictGroups.Add(
                key,
                conflictGroup);
        }

        conflictGroup.Register(
            rowNumber,
            productName);
    }

    private static CatalogImportNormalizedRowData?
        DeserializeNormalizedData(
            CatalogImportRow row)
    {
        try
        {
            return JsonSerializer.Deserialize<
                CatalogImportNormalizedRowData>(
                    row.NormalizedDataJson,
                    JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool AreEquivalent(
        CharacteristicDefinition definition,
        string explicitValue,
        string recognizedValue)
    {
        switch (definition.DataType)
        {
            case CharacteristicDataType.Number:
                return AreNumbersEquivalent(
                    explicitValue,
                    recognizedValue);

            case CharacteristicDataType.Boolean:
                return AreBooleansEquivalent(
                    definition.Code,
                    explicitValue,
                    recognizedValue);

            case CharacteristicDataType.Text:
                return AreTextValuesEquivalent(
                    definition.Code,
                    explicitValue,
                    recognizedValue);

            default:
                return false;
        }
    }

    private static bool AreNumbersEquivalent(
        string explicitValue,
        string recognizedValue)
    {
        var explicitParsed = decimal.TryParse(
            explicitValue,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var explicitNumber);

        var recognizedParsed = decimal.TryParse(
            recognizedValue,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var recognizedNumber);

        return explicitParsed &&
               recognizedParsed &&
               explicitNumber == recognizedNumber;
    }

    private static bool AreBooleansEquivalent(
        string characteristicCode,
        string explicitValue,
        string recognizedValue)
    {
        return CatalogCharacteristicBooleanValueNormalizer.TryNormalize(
                   characteristicCode,
                   explicitValue,
                   out var explicitBoolean) &&
               CatalogCharacteristicBooleanValueNormalizer.TryNormalize(
                   characteristicCode,
                   recognizedValue,
                   out var recognizedBoolean) &&
               explicitBoolean == recognizedBoolean;
    }

    private static bool AreTextValuesEquivalent(
        string characteristicCode,
        string explicitValue,
        string recognizedValue)
    {
        return string.Equals(
            NormalizeTextValue(
                characteristicCode,
                explicitValue),
            NormalizeTextValue(
                characteristicCode,
                recognizedValue),
            StringComparison.Ordinal);
    }

    private static string NormalizeComparableValue(
        CharacteristicDefinition definition,
        string value)
    {
        switch (definition.DataType)
        {
            case CharacteristicDataType.Number:
                if (decimal.TryParse(
                        value,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out var number))
                {
                    return number.ToString(
                        "0.############################",
                        CultureInfo.InvariantCulture);
                }

                return CatalogRecognitionTextNormalizer
                    .NormalizeValue(value);

            case CharacteristicDataType.Boolean:
                if (CatalogCharacteristicBooleanValueNormalizer.TryNormalize(
                        definition.Code,
                        value,
                        out var boolean))
                {
                    return boolean
                        ? "TRUE"
                        : "FALSE";
                }

                return CatalogRecognitionTextNormalizer.NormalizeValue(value);

            case CharacteristicDataType.Text:
                return NormalizeTextValue(
                    definition.Code,
                    value);

            default:
                return CatalogRecognitionTextNormalizer
                    .NormalizeValue(value);
        }
    }

    private static string NormalizeTextValue(
        string characteristicCode,
        string value)
    {
        var normalizedCode =
            CatalogRecognitionTextNormalizer.NormalizeCode(
                characteristicCode);

        if (string.Equals(
                normalizedCode,
                "CURVE",
                StringComparison.Ordinal))
        {
            return CatalogRecognitionTextNormalizer
                .NormalizeCurve(value);
        }

        if (string.Equals(
                normalizedCode,
                "IP_RATING",
                StringComparison.Ordinal))
        {
            return RemoveSpaces(
                CatalogRecognitionTextNormalizer.NormalizeValue(
                    value));
        }

        return CatalogRecognitionTextNormalizer
            .NormalizeValue(value);
    }

    private static string RemoveSpaces(string value)
    {
        return value.Replace(
            " ",
            string.Empty,
            StringComparison.Ordinal);
    }

    private sealed record ConflictGroupKey(
        string CharacteristicCode,
        string ExcelValue,
        string RecognizedValue,
        CatalogRecognitionSource RecognitionSource,
        string RecognizerKey);

    private sealed class MutableConflictGroup
    {
        private readonly List<int> _exampleRowNumbers = [];

        private readonly List<string> _exampleProductNames = [];

        public MutableConflictGroup(
            string characteristicCode,
            string characteristicName,
            string excelValue,
            string recognizedValue,
            string rawRecognizedValue,
            CatalogRecognitionSource recognitionSource,
            decimal confidence,
            int spanStart,
            int spanLength,
            int priority,
            string recognizerKey)
        {
            CharacteristicCode = characteristicCode;
            CharacteristicName = characteristicName;
            ExcelValue = excelValue;
            RecognizedValue = recognizedValue;
            RawRecognizedValue = rawRecognizedValue;
            RecognitionSource = recognitionSource;
            Confidence = confidence;
            SpanStart = spanStart;
            SpanLength = spanLength;
            Priority = priority;
            RecognizerKey = recognizerKey;
        }

        public string CharacteristicCode { get; }

        public string CharacteristicName { get; }

        public string ExcelValue { get; }

        public string RecognizedValue { get; }

        public string RawRecognizedValue { get; }

        public CatalogRecognitionSource RecognitionSource { get; }

        public decimal Confidence { get; }

        public int SpanStart { get; }

        public int SpanLength { get; }

        public int Priority { get; }

        public string RecognizerKey { get; }

        public int OccurrenceCount { get; private set; }

        public void Register(
            int rowNumber,
            string productName)
        {
            OccurrenceCount++;

            if (_exampleRowNumbers.Count <
                MaximumExamplesPerConflictGroup)
            {
                _exampleRowNumbers.Add(rowNumber);
            }

            if (_exampleProductNames.Count >=
                MaximumExamplesPerConflictGroup)
            {
                return;
            }

            if (_exampleProductNames.Contains(
                    productName,
                    StringComparer.Ordinal))
            {
                return;
            }

            _exampleProductNames.Add(productName);
        }

        public CatalogImportRecognitionShadowConflictGroup ToResult()
        {
            return new CatalogImportRecognitionShadowConflictGroup(
                CharacteristicCode,
                CharacteristicName,
                ExcelValue,
                RecognizedValue,
                RawRecognizedValue,
                RecognitionSource,
                Confidence,
                SpanStart,
                SpanLength,
                Priority,
                RecognizerKey,
                OccurrenceCount,
                _exampleRowNumbers.ToArray(),
                _exampleProductNames.ToArray());
        }
    }

    private sealed class MutableCharacteristicStatistics
    {
        public int ExplicitValuesCount { get; set; }

        public int RecognizedValuesCount { get; set; }

        public int MatchesCount { get; set; }

        public int ConflictsCount { get; set; }

        public int NotRecognizedCount { get; set; }

        public int RecognitionWithoutExplicitValueCount { get; set; }

        public int AmbiguousCount { get; set; }
    }
}