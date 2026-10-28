using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.PreviewExampleFill;

public sealed class PreviewCatalogImportExampleFillQueryHandler
{
    public const int MaximumTargetRows = 2_000;
    public const int MaximumExampleRows = 200;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly ICatalogImportExampleFillReader _exampleFillReader;
    private readonly IUserRepository _userRepository;

    public PreviewCatalogImportExampleFillQueryHandler(
        ICatalogImportBatchRepository importBatchRepository,
        ICatalogImportExampleFillReader exampleFillReader,
        IUserRepository userRepository)
    {
        ArgumentNullException.ThrowIfNull(importBatchRepository);
        ArgumentNullException.ThrowIfNull(exampleFillReader);
        ArgumentNullException.ThrowIfNull(userRepository);

        _importBatchRepository = importBatchRepository;
        _exampleFillReader = exampleFillReader;
        _userRepository = userRepository;
    }

    public async Task<Result<PreviewCatalogImportExampleFillResult, DomainError>> Handle(
        PreviewCatalogImportExampleFillQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<PreviewCatalogImportExampleFillResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (query.BatchId == Guid.Empty)
        {
            return Result.Failure<PreviewCatalogImportExampleFillResult, DomainError>(
                CatalogImportErrors.BatchNotFound(query.BatchId));
        }

        var exampleRowIds = NormalizeIds(query.ExampleRowIds);
        var targetRowIds = NormalizeIds(query.TargetRowIds);

        if (exampleRowIds.Length > MaximumExampleRows)
        {
            return Result.Failure<PreviewCatalogImportExampleFillResult, DomainError>(
                CatalogImportErrors.ExampleFillExampleLimitExceeded(MaximumExampleRows));
        }

        if (targetRowIds.Length > MaximumTargetRows)
        {
            return Result.Failure<PreviewCatalogImportExampleFillResult, DomainError>(
                CatalogImportErrors.ExampleFillTargetLimitExceeded(MaximumTargetRows));
        }

        var currentUser = await _userRepository
            .GetByIdAsync(query.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<PreviewCatalogImportExampleFillResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanEditCatalogImport())
        {
            return Result.Failure<PreviewCatalogImportExampleFillResult, DomainError>(
                CatalogImportErrors.UserCannotEditCatalogImport());
        }

        var batch = await _importBatchRepository
            .GetByIdAsync(query.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<PreviewCatalogImportExampleFillResult, DomainError>(
                CatalogImportErrors.BatchNotFound(query.BatchId));
        }

        if (batch.CreatedByUserId != currentUser.Id)
        {
            return Result.Failure<PreviewCatalogImportExampleFillResult, DomainError>(
                CatalogImportErrors.UserCannotAccessBatch());
        }

        if (!batch.CanEditRows)
        {
            return Result.Failure<PreviewCatalogImportExampleFillResult, DomainError>(
                CatalogImportErrors.BatchRowsCannotBeEdited(batch.Status));
        }

        if (batch.Version != query.ExpectedVersion)
        {
            return Result.Failure<PreviewCatalogImportExampleFillResult, DomainError>(
                CatalogImportErrors.BatchConcurrencyConflict());
        }

        var samples = await _exampleFillReader
            .GetActiveSamplesAsync(
                query.BatchId,
                exampleRowIds.Length == 0 ? null : exampleRowIds,
                cancellationToken)
            .ConfigureAwait(false);

        if (samples.Count == 0)
        {
            return Result.Failure<PreviewCatalogImportExampleFillResult, DomainError>(
                CatalogImportErrors.ExampleFillRequiresConfirmedExamples());
        }

        IReadOnlyCollection<CatalogImportRow> rows;
        if (targetRowIds.Length > 0)
        {
            rows = await _importBatchRepository
                .GetRowsByIdsAsync(query.BatchId, targetRowIds, cancellationToken)
                .ConfigureAwait(false);

            if (rows.Count != targetRowIds.Length)
            {
                var foundIds = rows.Select(row => row.Id).ToHashSet();
                var missingId = targetRowIds.First(id => !foundIds.Contains(id));
                return Result.Failure<PreviewCatalogImportExampleFillResult, DomainError>(
                    CatalogImportErrors.RowNotFound(missingId));
            }
        }
        else
        {
            rows = await _importBatchRepository
                .GetRowsAsync(
                    query.BatchId,
                    null,
                    null,
                    null,
                    null,
                    null,
                    0,
                    MaximumTargetRows + 1,
                    cancellationToken)
                .ConfigureAwait(false);

            if (rows.Count > MaximumTargetRows)
            {
                return Result.Failure<PreviewCatalogImportExampleFillResult, DomainError>(
                    CatalogImportErrors.ExampleFillTargetLimitExceeded(MaximumTargetRows));
            }
        }

        var samplesByScope = samples
            .GroupBy(sample => (sample.ManufacturerId, sample.ProductTypeId))
            .ToDictionary(group => group.Key, group => group.ToArray());

        var resultRows = new List<PreviewCatalogImportExampleFillRowResult>();

        foreach (var row in rows.OrderBy(item => item.RowNumber))
        {
            var data = Deserialize(row.NormalizedDataJson);
            if (data is null)
            {
                return Result.Failure<PreviewCatalogImportExampleFillResult, DomainError>(
                    CatalogImportErrors.InvalidImportJson(nameof(CatalogImportRow.NormalizedDataJson)));
            }

            if (string.IsNullOrWhiteSpace(data.Name))
            {
                resultRows.Add(CreateUnmatchedRow(
                    row,
                    string.Empty,
                    "Не указано наименование товара."));
                continue;
            }

            if (data.ManufacturerId is not Guid manufacturerId)
            {
                resultRows.Add(CreateUnmatchedRow(
                    row,
                    data.Name,
                    "Не определён производитель. Примеры применяются только внутри одного производителя."));
                continue;
            }

            if (data.ProductTypeId is not Guid productTypeId)
            {
                resultRows.Add(CreateUnmatchedRow(
                    row,
                    data.Name,
                    "Не определён тип товара. Примеры применяются только внутри одного типа товара."));
                continue;
            }

            if (!samplesByScope.TryGetValue(
                    (manufacturerId, productTypeId),
                    out var scopedSamples))
            {
                resultRows.Add(CreateUnmatchedRow(
                    row,
                    data.Name,
                    "Среди выбранных строк нет подтверждённых примеров для этого производителя и типа товара."));
                continue;
            }

            var applicableSamples = scopedSamples
                .Where(sample => sample.SourceRowId != row.Id)
                .ToArray();

            var matchedSamples = applicableSamples
                .Select(sample => new
                {
                    Sample = sample,
                    MatchStart = CatalogImportExampleFillTokenMatcher.FindStart(
                        data.Name,
                        sample.RawValue,
                        sample.CharacteristicUnit)
                })
                .Where(item => item.MatchStart >= 0)
                .GroupBy(item => item.Sample.CharacteristicDefinitionId)
                .ToArray();

            if (matchedSamples.Length == 0)
            {
                var reason = applicableSamples.Length == 0 &&
                    exampleRowIds.Contains(row.Id)
                    ? "Строка выбрана как источник примера и не заполняется по собственному примеру."
                    : BuildNoMatchReason(applicableSamples);

                resultRows.Add(CreateUnmatchedRow(
                    row,
                    data.Name,
                    reason));
                continue;
            }

            var proposals = new List<PreviewCatalogImportExampleFillProposalResult>();
            var conflicts = new List<PreviewCatalogImportExampleFillConflictResult>();

            foreach (var characteristicMatches in matchedSamples)
            {
                var matches = characteristicMatches.Select(item => item.Sample).ToArray();
                var proposedValues = matches
                    .Select(sample => sample.NormalizedValue.Trim())
                    .Where(value => value.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var currentValue = FindCharacteristicValue(
                    data.Characteristics,
                    characteristicMatches.Key);

                if (proposedValues.Length != 1)
                {
                    conflicts.Add(new PreviewCatalogImportExampleFillConflictResult(
                        characteristicMatches.Key,
                        matches[0].CharacteristicName,
                        "Один и тот же фрагмент в подтверждённых примерах связан с разными значениями.",
                        currentValue,
                        proposedValues,
                        matches.Select(sample => sample.ExampleId).Distinct().ToArray()));
                    continue;
                }

                var proposedValue = proposedValues[0];
                if (!string.IsNullOrWhiteSpace(currentValue))
                {
                    if (!string.Equals(currentValue.Trim(), proposedValue, StringComparison.OrdinalIgnoreCase))
                    {
                        conflicts.Add(new PreviewCatalogImportExampleFillConflictResult(
                            characteristicMatches.Key,
                            matches[0].CharacteristicName,
                            "В строке уже сохранено другое значение характеристики.",
                            currentValue,
                            [proposedValue],
                            matches.Select(sample => sample.ExampleId).Distinct().ToArray()));
                    }

                    continue;
                }

                var supportingExamples = matches
                    .Where(sample => string.Equals(
                        sample.NormalizedValue.Trim(),
                        proposedValue,
                        StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var confidence = supportingExamples
                    .Select(sample => sample.SourceRowId)
                    .Distinct()
                    .Count() >= 2
                    ? 1.0000m
                    : 0.8500m;

                proposals.Add(new PreviewCatalogImportExampleFillProposalResult(
                    characteristicMatches.Key,
                    matches[0].CharacteristicName,
                    proposedValue,
                    currentValue,
                    supportingExamples
                        .OrderByDescending(sample => sample.RawValue.Length)
                        .First()
                        .RawValue,
                    confidence,
                    supportingExamples.Select(sample => sample.ExampleId).Distinct().ToArray(),
                    supportingExamples.Select(sample => sample.SourceRowId).Distinct().ToArray()));
            }

            if (proposals.Count == 0 && conflicts.Count == 0)
            {
                resultRows.Add(CreateUnmatchedRow(
                    row,
                    data.Name,
                    "Подходящие фрагменты найдены, но соответствующие характеристики уже заполнены теми же значениями."));
                continue;
            }

            var state = "Review";
            if (conflicts.Count > 0)
            {
                state = "Conflict";
            }
            else if (proposals.All(proposal => proposal.Confidence >= 1m))
            {
                state = "Ready";
            }

            resultRows.Add(new PreviewCatalogImportExampleFillRowResult(
                row.Id,
                row.RowNumber,
                data.Name,
                state,
                null,
                proposals,
                conflicts));
        }

        var readyRowsCount = resultRows.Count(row => string.Equals(row.State, "Ready", StringComparison.Ordinal));
        var reviewRowsCount = resultRows.Count(row => string.Equals(row.State, "Review", StringComparison.Ordinal));
        var conflictRowsCount = resultRows.Count(row => string.Equals(row.State, "Conflict", StringComparison.Ordinal));
        var unmatchedRowsCount = resultRows.Count(row => string.Equals(row.State, "Unmatched", StringComparison.Ordinal));

        return Result.Success<PreviewCatalogImportExampleFillResult, DomainError>(
            new PreviewCatalogImportExampleFillResult(
                batch.Version,
                samples.Count,
                rows.Count,
                readyRowsCount,
                reviewRowsCount,
                conflictRowsCount,
                unmatchedRowsCount,
                resultRows));
    }

    private static Guid[] NormalizeIds(IReadOnlyCollection<Guid>? ids) =>
        ids?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray()
        ?? [];

    private static CatalogImportNormalizedRowData? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<CatalogImportNormalizedRowData>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static string? FindCharacteristicValue(
        IReadOnlyDictionary<string, string> characteristics,
        Guid characteristicDefinitionId)
    {
        var key = characteristicDefinitionId.ToString();
        if (characteristics.TryGetValue(key, out var value))
        {
            return value;
        }

        return characteristics
            .FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
            .Value;
    }

    private static PreviewCatalogImportExampleFillRowResult CreateUnmatchedRow(
        CatalogImportRow row,
        string productName,
        string reason)
    {
        return new PreviewCatalogImportExampleFillRowResult(
            row.Id,
            row.RowNumber,
            productName,
            "Unmatched",
            reason,
            [],
            []);
    }

    private static string BuildNoMatchReason(
        CatalogImportExampleFillSample[] samples)
    {
        if (samples.Length == 0)
        {
            return "Нет другого подтверждённого примера, который можно применить к этой строке.";
        }

        var fragments = samples
            .Select(sample => sample.RawValue.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .Select(value => $"«{value}»")
            .ToArray();

        return fragments.Length == 0
            ? "В наименовании не найдено ни одного подтверждённого фрагмента."
            : $"В наименовании не найдены подтверждённые фрагменты: {string.Join(", ", fragments)}.";
    }

}
