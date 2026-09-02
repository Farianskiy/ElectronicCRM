using System.Text.Json;
using ElectronicService.Core.Catalog.ProductTypes.Suggestions;
using ElectronicService.Core.Catalog.ProductNames.Explanation;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed class CatalogImportProductTypeSuggestionShadowService
    : ICatalogImportProductTypeSuggestionShadowService
{
    private const int MaximumSamples = 100;
    private const int MaximumSamplesPerKind = 100;
    private const int MaximumExampleRowsPerGroup = 10;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly ICatalogProductTypeSuggestionService _suggestionService;

    public CatalogImportProductTypeSuggestionShadowService(
        ICatalogProductTypeSuggestionService suggestionService)
    {
        ArgumentNullException.ThrowIfNull(suggestionService);

        _suggestionService = suggestionService;
    }

    public async Task<CatalogImportProductTypeSuggestionShadowResult> AnalyzeAsync(
        CatalogImportWorkbookAnalysis analysis,
        ProductType? selectedProductType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        cancellationToken.ThrowIfCancellationRequested();

        var suggestionIndex = await _suggestionService
            .LoadIndexAsync(cancellationToken)
            .ConfigureAwait(false);

        var rowsAnalyzedCount = 0;
        var suggestedRowsCount = 0;
        var matchesCount = 0;
        var conflictsCount = 0;
        var suggestionsCount = 0;
        var nameConflictsCount = 0;
        var unresolvedCount = 0;

        var typeGroups =
            new Dictionary<Guid, MutableProductTypeGroup>();

        var evidenceRows =
            new List<CatalogImportProductNameEvidenceRow>();

        var sampleBuckets =
            new Dictionary<
                CatalogImportProductTypeSuggestionShadowSampleKind,
                List<CatalogImportProductTypeSuggestionShadowSample>>();

        foreach (var row in analysis.Rows.OrderBy(row => row.RowNumber))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var data = DeserializeNormalizedData(row);

            if (data is null ||
                string.IsNullOrWhiteSpace(data.Name))
            {
                continue;
            }

            rowsAnalyzedCount++;

            var suggestion = suggestionIndex.Suggest(
                data.Name,
                cancellationToken);

            var rowEvidence = suggestion.Candidates
                .SelectMany(candidate =>
                    candidate.Evidence.Select(evidence =>
                        MapExplanationEvidence(
                            candidate,
                            evidence)))
                .ToArray();

            if (rowEvidence.Length > 0)
            {
                evidenceRows.Add(
                    new CatalogImportProductNameEvidenceRow(
                        row.RowNumber,
                        rowEvidence));
            }

            if (suggestion.IsConflict)
            {
                nameConflictsCount++;

                AddSample(
                    sampleBuckets,
                    CreateSample(
                        row.RowNumber,
                        CatalogImportProductTypeSuggestionShadowSampleKind.NameConflict,
                        data.Name,
                        selectedProductType,
                        suggestion,
                        "В наименовании найдены разные типы товара с одинаковым наивысшим приоритетом. Shadow Mode не выбирает один из них."));

                continue;
            }

            if (!suggestion.IsSuggested)
            {
                unresolvedCount++;

                AddSample(
                    sampleBuckets,
                    CreateSample(
                        row.RowNumber,
                        CatalogImportProductTypeSuggestionShadowSampleKind.Unresolved,
                        data.Name,
                        selectedProductType,
                        suggestion,
                        "В наименовании не найден Approved-термин типа товара. Shadow Mode оставляет результат неопределённым."));

                continue;
            }

            var selectedCandidate =
                suggestion.SelectedCandidate
                ?? throw new InvalidOperationException(
                    "Suggested product type result does not contain a selected candidate.");

            suggestedRowsCount++;

            CatalogImportProductTypeSuggestionShadowSampleKind sampleKind;
            string details;

            if (selectedProductType is null)
            {
                suggestionsCount++;

                sampleKind =
                    CatalogImportProductTypeSuggestionShadowSampleKind
                        .Suggestion;

                details =
                    $"Для пакета тип товара ещё не выбран. В наименовании предложен тип '{selectedCandidate.ProductTypeName}', но Shadow Mode не назначает ProductTypeId автоматически.";
            }
            else if (selectedProductType.Id ==
                     selectedCandidate.ProductTypeId)
            {
                matchesCount++;

                sampleKind =
                    CatalogImportProductTypeSuggestionShadowSampleKind
                        .Match;

                details =
                    $"Выбранный тип пакета '{selectedProductType.Name}' совпадает с типом, предложенным по наименованию.";
            }
            else
            {
                conflictsCount++;

                sampleKind =
                    CatalogImportProductTypeSuggestionShadowSampleKind
                        .Conflict;

                details =
                    $"Выбранный тип пакета '{selectedProductType.Name}' отличается от типа '{selectedCandidate.ProductTypeName}', предложенного по наименованию. ProductTypeId пакета остаётся авторитетным.";
            }

            AddToTypeGroup(
                typeGroups,
                selectedCandidate,
                row.RowNumber,
                sampleKind);

            AddSample(
                sampleBuckets,
                CreateSample(
                    row.RowNumber,
                    sampleKind,
                    data.Name,
                    selectedProductType,
                    suggestion,
                    details));
        }

        var groups = typeGroups.Values
            .Select(group => group.ToResult())
            .OrderByDescending(group => group.RowsCount)
            .ThenByDescending(group => group.HighestConfidence)
            .ThenBy(
                group => group.ProductTypeCode,
                StringComparer.Ordinal)
            .ToArray();

        var samples = sampleBuckets
            .SelectMany(pair => pair.Value)
            .OrderBy(sample => sample.Kind)
            .ThenBy(sample => sample.RowNumber)
            .Take(MaximumSamples)
            .ToArray();

        return new CatalogImportProductTypeSuggestionShadowResult(
            rowsAnalyzedCount,
            selectedProductType is not null,
            selectedProductType?.Id,
            selectedProductType?.Code,
            selectedProductType?.Name,
            suggestedRowsCount,
            matchesCount,
            conflictsCount,
            suggestionsCount,
            nameConflictsCount,
            unresolvedCount,
            groups.Length,
            groups.Length > 1,
            rowsAnalyzedCount > samples.Length,
            groups,
            samples,
            evidenceRows);
    }

    private static void AddToTypeGroup(
        Dictionary<Guid, MutableProductTypeGroup> typeGroups,
        CatalogProductTypeSuggestionCandidate candidate,
        int rowNumber,
        CatalogImportProductTypeSuggestionShadowSampleKind sampleKind)
    {
        if (!typeGroups.TryGetValue(
                candidate.ProductTypeId,
                out var group))
        {
            group = new MutableProductTypeGroup(candidate);

            typeGroups.Add(
                candidate.ProductTypeId,
                group);
        }

        group.Add(
            rowNumber,
            sampleKind,
            MaximumExampleRowsPerGroup);
    }

    private static void AddSample(
    Dictionary<
        CatalogImportProductTypeSuggestionShadowSampleKind,
        List<CatalogImportProductTypeSuggestionShadowSample>>
        sampleBuckets,
    CatalogImportProductTypeSuggestionShadowSample sample)
    {
        if (!sampleBuckets.TryGetValue(
                sample.Kind,
                out var samples))
        {
            samples = [];

            sampleBuckets.Add(
                sample.Kind,
                samples);
        }

        if (samples.Count < MaximumSamplesPerKind)
        {
            samples.Add(sample);
        }
    }

    private static CatalogImportProductTypeSuggestionShadowSample CreateSample(
        int rowNumber,
        CatalogImportProductTypeSuggestionShadowSampleKind kind,
        string productName,
        ProductType? selectedProductType,
        CatalogProductTypeSuggestionResult suggestion,
        string details)
    {
        var selectedCandidate = suggestion.SelectedCandidate;

        var candidates = suggestion.Candidates
            .Select(MapCandidate)
            .ToArray();

        return new CatalogImportProductTypeSuggestionShadowSample(
            rowNumber,
            kind,
            productName,
            selectedProductType?.Id,
            selectedProductType?.Code,
            selectedProductType?.Name,
            selectedCandidate?.ProductTypeId,
            selectedCandidate?.ProductTypeCode,
            selectedCandidate?.ProductTypeName,
            selectedCandidate?.Confidence,
            selectedCandidate?.HighestPriority,
            candidates,
            details);
    }

    private static CatalogProductNameEvidenceSpan MapExplanationEvidence(
    CatalogProductTypeSuggestionCandidate candidate,
    CatalogProductTypeSuggestionEvidence evidence)
    {
        return new CatalogProductNameEvidenceSpan(
            CatalogProductNameEvidenceKind.ProductType,
            candidate.ProductTypeCode,
            candidate.ProductTypeName,
            evidence.RawValue,
            evidence.Source,
            candidate.Confidence,
            evidence.Priority,
            evidence.StartIndex,
            evidence.Length);
    }


    private static CatalogImportProductTypeSuggestionShadowCandidate MapCandidate(
        CatalogProductTypeSuggestionCandidate candidate)
    {
        return new CatalogImportProductTypeSuggestionShadowCandidate(
            candidate.ProductTypeId,
            candidate.ProductTypeCode,
            candidate.ProductTypeName,
            candidate.HighestPriority,
            candidate.Confidence,
            candidate.Evidence
                .Select(MapEvidence)
                .ToArray());
    }

    private static CatalogImportProductTypeSuggestionShadowEvidence MapEvidence(
        CatalogProductTypeSuggestionEvidence evidence)
    {
        return new CatalogImportProductTypeSuggestionShadowEvidence(
            evidence.DictionaryTermId,
            evidence.Phrase,
            evidence.RawValue,
            evidence.NormalizedValue,
            evidence.Priority,
            evidence.Source,
            evidence.StartIndex,
            evidence.Length);
    }

    private static CatalogImportNormalizedRowData? DeserializeNormalizedData(
        CatalogImportRow row)
    {
        try
        {
            return JsonSerializer.Deserialize<CatalogImportNormalizedRowData>(
                row.NormalizedDataJson,
                JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class MutableProductTypeGroup
    {
        private readonly CatalogProductTypeSuggestionCandidate _candidate;
        private readonly List<int> _exampleRowNumbers = [];

        private int _rowsCount;
        private int _matchesCount;
        private int _conflictsCount;
        private int _suggestionsCount;
        private decimal _highestConfidence;

        public MutableProductTypeGroup(
            CatalogProductTypeSuggestionCandidate candidate)
        {
            ArgumentNullException.ThrowIfNull(candidate);

            _candidate = candidate;
            _highestConfidence = candidate.Confidence;
        }

        public void Add(
            int rowNumber,
            CatalogImportProductTypeSuggestionShadowSampleKind sampleKind,
            int maximumExampleRows)
        {
            _rowsCount++;

            _highestConfidence = Math.Max(
                _highestConfidence,
                _candidate.Confidence);

            switch (sampleKind)
            {
                case CatalogImportProductTypeSuggestionShadowSampleKind.Match:
                    _matchesCount++;
                    break;

                case CatalogImportProductTypeSuggestionShadowSampleKind.Conflict:
                    _conflictsCount++;
                    break;

                case CatalogImportProductTypeSuggestionShadowSampleKind.Suggestion:
                    _suggestionsCount++;
                    break;
            }

            if (_exampleRowNumbers.Count < maximumExampleRows)
            {
                _exampleRowNumbers.Add(rowNumber);
            }
        }

        public CatalogImportProductTypeSuggestionShadowGroup ToResult()
        {
            return new CatalogImportProductTypeSuggestionShadowGroup(
                _candidate.ProductTypeId,
                _candidate.ProductTypeCode,
                _candidate.ProductTypeName,
                _rowsCount,
                _matchesCount,
                _conflictsCount,
                _suggestionsCount,
                _highestConfidence,
                _exampleRowNumbers.ToArray());
        }
    }
}