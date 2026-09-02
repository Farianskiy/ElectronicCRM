using System.Text.Json;
using ElectronicService.Core.Catalog.Manufacturers.Resolution;
using ElectronicService.Core.Catalog.ProductNames.Explanation;
using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed class CatalogImportManufacturerRecognitionShadowService
    : ICatalogImportManufacturerRecognitionShadowService
{
    private const int MaximumSamples = 100;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public CatalogImportManufacturerRecognitionShadowResult Analyze(
        CatalogImportWorkbookAnalysis analysis,
        ManufacturerResolutionIndex manufacturerResolutionIndex,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(manufacturerResolutionIndex);

        var rowsAnalyzedCount = 0;
        var rowsWithExcelManufacturerValueCount = 0;
        var rowsWithResolvedExcelManufacturerCount = 0;
        var rowsWithRecognizedManufacturerCount = 0;
        var matchesCount = 0;
        var conflictsCount = 0;
        var suggestionsCount = 0;
        var nameConflictsCount = 0;
        var nameUnresolvedCount = 0;
        var comparisonUnavailableCount = 0;

        var samples =
            new List<CatalogImportManufacturerRecognitionShadowSample>();

        var evidenceRows =
            new List<CatalogImportProductNameEvidenceRow>();

        foreach (var row in analysis.Rows.OrderBy(row => row.RowNumber))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var data = DeserializeNormalizedData(row);

            if (data is null || string.IsNullOrWhiteSpace(data.Name))
            {
                continue;
            }

            rowsAnalyzedCount++;

            var hasExcelManufacturer =
                !string.IsNullOrWhiteSpace(data.Manufacturer);

            var hasResolvedExcelManufacturer =
                data.ManufacturerId.HasValue &&
                data.ManufacturerId.Value != Guid.Empty;

            if (hasExcelManufacturer)
            {
                rowsWithExcelManufacturerValueCount++;
            }

            if (hasResolvedExcelManufacturer)
            {
                rowsWithResolvedExcelManufacturerCount++;
            }

            var recognition =
                manufacturerResolutionIndex.RecognizeInText(data.Name);

            var rowEvidence = recognition.Candidates
                .Select(MapEvidence)
                .ToArray();

            if (rowEvidence.Length > 0)
            {
                evidenceRows.Add(
                    new CatalogImportProductNameEvidenceRow(
                        row.RowNumber,
                        rowEvidence));
            }

            if (recognition.IsConflict)
            {
                nameConflictsCount++;

                samples.Add(
                    CreateSample(
                        row.RowNumber,
                        CatalogImportManufacturerRecognitionShadowSampleKind.NameConflict,
                        data,
                        recognition,
                        "В наименовании найдены разные производители. Shadow Mode не выбирает одного из них."));

                continue;
            }

            if (!recognition.IsResolved)
            {
                nameUnresolvedCount++;

                continue;
            }

            rowsWithRecognizedManufacturerCount++;

            var selectedCandidate =
                recognition.SelectedCandidate
                ?? throw new InvalidOperationException(
                    "Resolved manufacturer recognition does not contain a selected candidate.");

            if (!hasExcelManufacturer)
            {
                suggestionsCount++;

                samples.Add(
                    CreateSample(
                        row.RowNumber,
                        CatalogImportManufacturerRecognitionShadowSampleKind.Suggestion,
                        data,
                        recognition,
                        $"В Excel производитель не заполнен. В наименовании найден производитель '{selectedCandidate.ManufacturerName}', но Shadow Mode не записывает его в строку."));

                continue;
            }

            if (!hasResolvedExcelManufacturer)
            {
                comparisonUnavailableCount++;

                samples.Add(
                    CreateSample(
                        row.RowNumber,
                        CatalogImportManufacturerRecognitionShadowSampleKind.ComparisonUnavailable,
                        data,
                        recognition,
                        $"В Excel указано значение производителя '{data.Manufacturer}', но оно не разрешилось в каноническую запись. В наименовании найден производитель '{selectedCandidate.ManufacturerName}'. Автоматическая замена не выполняется."));

                continue;
            }

            if (data.ManufacturerId == selectedCandidate.ManufacturerId)
            {
                matchesCount++;

                samples.Add(
                    CreateSample(
                        row.RowNumber,
                        CatalogImportManufacturerRecognitionShadowSampleKind.Match,
                        data,
                        recognition,
                        $"Производитель Excel совпадает с производителем '{selectedCandidate.ManufacturerName}', найденным в наименовании."));

                continue;
            }

            conflictsCount++;

            samples.Add(
                CreateSample(
                    row.RowNumber,
                    CatalogImportManufacturerRecognitionShadowSampleKind.Conflict,
                    data,
                    recognition,
                    $"Производитель Excel '{data.Manufacturer}' отличается от производителя '{selectedCandidate.ManufacturerName}', найденного в наименовании. Значение Excel остаётся авторитетным."));
        }

        var orderedSamples = samples
            .OrderBy(sample => sample.Kind)
            .ThenBy(sample => sample.RowNumber)
            .Take(MaximumSamples)
            .ToArray();

        return new CatalogImportManufacturerRecognitionShadowResult(
            rowsAnalyzedCount,
            rowsWithExcelManufacturerValueCount,
            rowsWithResolvedExcelManufacturerCount,
            rowsWithRecognizedManufacturerCount,
            matchesCount,
            conflictsCount,
            suggestionsCount,
            nameConflictsCount,
            nameUnresolvedCount,
            comparisonUnavailableCount,
            samples.Count > MaximumSamples,
            orderedSamples,
            evidenceRows);
    }

    private static CatalogProductNameEvidenceSpan MapEvidence(
    ManufacturerNameRecognitionCandidate candidate)
    {
        return new CatalogProductNameEvidenceSpan(
            CatalogProductNameEvidenceKind.Manufacturer,
            "MANUFACTURER",
            candidate.ManufacturerName,
            candidate.RawValue,
            candidate.Source.ToString(),
            candidate.Confidence,
            0,
            candidate.StartIndex,
            candidate.Length);
    }


    private static CatalogImportManufacturerRecognitionShadowSample CreateSample(
        int rowNumber,
        CatalogImportManufacturerRecognitionShadowSampleKind kind,
        CatalogImportNormalizedRowData data,
        ManufacturerNameRecognitionResult recognition,
        string details)
    {
        var selectedCandidate = recognition.SelectedCandidate;

        var candidates = recognition.Candidates
            .Select(candidate =>
                new CatalogImportManufacturerRecognitionShadowCandidate(
                    candidate.ManufacturerId,
                    candidate.ManufacturerName,
                    candidate.RawValue,
                    candidate.NormalizedValue,
                    candidate.Confidence,
                    candidate.Source,
                    candidate.ManufacturerAliasId,
                    candidate.StartIndex,
                    candidate.Length))
            .ToArray();

        return new CatalogImportManufacturerRecognitionShadowSample(
            rowNumber,
            kind,
            data.Name!,
            data.ManufacturerId,
            data.Manufacturer,
            data.ManufacturerResolutionSource,
            selectedCandidate?.ManufacturerId,
            selectedCandidate?.ManufacturerName,
            selectedCandidate?.RawValue,
            selectedCandidate?.Confidence,
            selectedCandidate?.Source,
            selectedCandidate?.StartIndex,
            selectedCandidate?.Length,
            candidates,
            details);
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