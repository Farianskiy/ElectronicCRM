using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public static class CatalogImportReanalysisRows
{
    public static Result<IReadOnlyCollection<CatalogImportRow>, DomainError> Prepare(
        IReadOnlyCollection<CatalogImportRow> generatedRows,
        IReadOnlyCollection<CatalogImportRow> preservedRows)
    {
        ArgumentNullException.ThrowIfNull(generatedRows);
        ArgumentNullException.ThrowIfNull(preservedRows);

        var generatedByNumber = generatedRows.ToLookup(row => row.RowNumber);
        var preservedByNumber = new Dictionary<int, CatalogImportRow>();

        foreach (var preservedRow in preservedRows)
        {
            var candidates = generatedByNumber[preservedRow.RowNumber].ToArray();
            if (candidates.Length != 1
                || candidates[0].BatchId != preservedRow.BatchId
                || !SameSource(preservedRow.RawDataJson, candidates[0].RawDataJson)
                || !preservedByNumber.TryAdd(preservedRow.RowNumber, preservedRow))
            {
                return new DomainError(
                    "catalog_import.reanalysis_source_changed",
                    $"Не удалось однозначно сопоставить исправленную строку {preservedRow.RowNumber} с исходным файлом. Повторный анализ отменён.");
            }
        }

        // Keep the saved identity, edits and field origins, but run recognition
        // for this row too. Existing values remain protected by enrichment.
        var rows = generatedRows
            .Select(row => preservedByNumber.GetValueOrDefault(row.RowNumber, row))
            .OrderBy(row => row.RowNumber)
            .ToArray();

        return Result.Success<IReadOnlyCollection<CatalogImportRow>, DomainError>(rows);
    }

    private static bool SameSource(string savedJson, string generatedJson)
    {
        try
        {
            using var saved = JsonDocument.Parse(savedJson);
            using var generated = JsonDocument.Parse(generatedJson);
            return JsonElement.DeepEquals(saved.RootElement, generated.RootElement);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
