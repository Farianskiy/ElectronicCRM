using System.Text.Json;
using ElectronicService.Core.Catalog.ProductNames.Explanation;
using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed class CatalogImportProductNameExplanationService :
    ICatalogImportProductNameExplanationService
{
    private const int MaximumSamples = 100;

    private const int MaximumSamplesPerKind = 100;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly ICatalogProductNameEvidenceCoverageService _coverageService;

    public CatalogImportProductNameExplanationService(
        ICatalogProductNameEvidenceCoverageService coverageService)
    {
        ArgumentNullException.ThrowIfNull(coverageService);

        _coverageService = coverageService;
    }

    public CatalogImportProductNameExplanationSummary Analyze(
        CatalogImportWorkbookAnalysis analysis,
        CatalogImportManufacturerRecognitionShadowResult manufacturerRecognitionShadow,
        CatalogImportProductTypeSuggestionShadowResult productTypeSuggestionShadow,
        CatalogImportRecognitionShadowResult? characteristicRecognitionShadow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(manufacturerRecognitionShadow);
        ArgumentNullException.ThrowIfNull(productTypeSuggestionShadow);

        var evidenceByRow =
            new Dictionary<
                int,
                List<CatalogProductNameEvidenceSpan>>();

        AddEvidenceRows(
            evidenceByRow,
            manufacturerRecognitionShadow.EvidenceRows);

        AddEvidenceRows(
            evidenceByRow,
            productTypeSuggestionShadow.EvidenceRows);

        if (characteristicRecognitionShadow is not null)
        {
            AddEvidenceRows(
                evidenceByRow,
                characteristicRecognitionShadow.EvidenceRows);
        }

        var sampleBuckets =
            new Dictionary<
                CatalogImportProductNameExplanationSampleKind,
                List<CatalogImportProductNameExplanationSample>>();

        var rowsAnalyzedCount = 0;
        var rowsWithEvidenceCount = 0;
        var fullyExplainedRowsCount = 0;
        var partiallyExplainedRowsCount = 0;
        var unexplainedRowsCount = 0;
        var coverageSum = 0.0000m;

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

            CatalogProductNameEvidenceSpan[] rowEvidence;

            if (evidenceByRow.TryGetValue(
                    row.RowNumber,
                    out var collectedEvidence))
            {
                rowEvidence = collectedEvidence
                    .Distinct()
                    .ToArray();
            }
            else
            {
                rowEvidence =
                    Array.Empty<CatalogProductNameEvidenceSpan>();
            }

            var explanation = _coverageService.Explain(
                data.Name,
                rowEvidence);

            coverageSum += explanation.Coverage;

            if (explanation.Evidence.Count > 0)
            {
                rowsWithEvidenceCount++;
            }

            var sampleKind =
                GetSampleKind(explanation);

            switch (sampleKind)
            {
                case CatalogImportProductNameExplanationSampleKind
                    .FullyExplained:
                    fullyExplainedRowsCount++;
                    break;

                case CatalogImportProductNameExplanationSampleKind
                    .PartiallyExplained:
                    partiallyExplainedRowsCount++;
                    break;

                case CatalogImportProductNameExplanationSampleKind
                    .Unexplained:
                    unexplainedRowsCount++;
                    break;

                case CatalogImportProductNameExplanationSampleKind.None:
                default:
                    throw new InvalidOperationException(
                        "Product name explanation sample kind was not determined.");
            }

            var sample =
                CreateSample(
                    row.RowNumber,
                    sampleKind,
                    explanation);

            AddSample(
                sampleBuckets,
                sample);
        }

        var samples = sampleBuckets
            .SelectMany(pair => pair.Value)
            .OrderBy(sample => sample.Kind)
            .ThenBy(sample => sample.Explanation.Coverage)
            .ThenBy(sample => sample.RowNumber)
            .Take(MaximumSamples)
            .ToArray();

        var averageCoverage =
            CalculateAverageCoverage(
                rowsAnalyzedCount,
                coverageSum);

        return new CatalogImportProductNameExplanationSummary(
            rowsAnalyzedCount,
            rowsWithEvidenceCount,
            fullyExplainedRowsCount,
            partiallyExplainedRowsCount,
            unexplainedRowsCount,
            averageCoverage,
            rowsAnalyzedCount > samples.Length,
            samples);
    }

    private static void AddEvidenceRows(
        Dictionary<
            int,
            List<CatalogProductNameEvidenceSpan>> evidenceByRow,
        IReadOnlyCollection<CatalogImportProductNameEvidenceRow> evidenceRows)
    {
        foreach (var evidenceRow in evidenceRows)
        {
            if (!evidenceByRow.TryGetValue(
                    evidenceRow.RowNumber,
                    out var collectedEvidence))
            {
                collectedEvidence = [];

                evidenceByRow.Add(
                    evidenceRow.RowNumber,
                    collectedEvidence);
            }

            collectedEvidence.AddRange(
                evidenceRow.Evidence);
        }
    }

    private static CatalogImportProductNameExplanationSample CreateSample(
        int rowNumber,
        CatalogImportProductNameExplanationSampleKind kind,
        CatalogProductNameExplanationResult explanation)
    {
        var manufacturerEvidenceCount =
            explanation.Evidence.Count(evidence =>
                evidence.Kind ==
                CatalogProductNameEvidenceKind.Manufacturer);

        var productTypeEvidenceCount =
            explanation.Evidence.Count(evidence =>
                evidence.Kind ==
                CatalogProductNameEvidenceKind.ProductType);

        var characteristicEvidenceCount =
            explanation.Evidence.Count(evidence =>
                evidence.Kind ==
                CatalogProductNameEvidenceKind.Characteristic);

        return new CatalogImportProductNameExplanationSample(
            rowNumber,
            kind,
            explanation,
            manufacturerEvidenceCount,
            productTypeEvidenceCount,
            characteristicEvidenceCount);
    }

    private static CatalogImportProductNameExplanationSampleKind GetSampleKind(
        CatalogProductNameExplanationResult explanation)
    {
        if (explanation.IsFullyExplained)
        {
            return CatalogImportProductNameExplanationSampleKind
                .FullyExplained;
        }

        if (explanation.CoveredMeaningfulCharactersCount == 0)
        {
            return CatalogImportProductNameExplanationSampleKind
                .Unexplained;
        }

        return CatalogImportProductNameExplanationSampleKind
            .PartiallyExplained;
    }

    private static void AddSample(
        Dictionary<
            CatalogImportProductNameExplanationSampleKind,
            List<CatalogImportProductNameExplanationSample>> sampleBuckets,
        CatalogImportProductNameExplanationSample sample)
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

    private static decimal CalculateAverageCoverage(
        int rowsAnalyzedCount,
        decimal coverageSum)
    {
        if (rowsAnalyzedCount == 0)
        {
            return 0.0000m;
        }

        return decimal.Round(
            coverageSum / rowsAnalyzedCount,
            4,
            MidpointRounding.AwayFromZero);
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
}