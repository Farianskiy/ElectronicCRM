using System.Text.Json;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.Manufacturers.Resolution;
using ElectronicService.Core.Catalog.ProductNames.Explanation;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.ProductTypes.Suggestions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Core.Catalog.Recognition.Normalization;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.GetRowExplanations;

public sealed class GetCatalogImportRowExplanationsQueryHandler(
    ICatalogImportBatchRepository batches,
    IUserRepository users,
    ICatalogProductMetadataRepository metadata,
    IManufacturerResolver manufacturers,
    ICatalogProductTypeSuggestionService productTypes,
    ICatalogProductNameRecognitionService recognition,
    ICatalogProductNameEvidenceCoverageService coverage)
{
    private const int MaximumRows = 25;
    private const int MaximumNameLength = 2000;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<
        Result<GetCatalogImportRowExplanationsResult, DomainError>> Handle(
        GetCatalogImportRowExplanationsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
            return CatalogImportErrors.CurrentUserNotFound();

        if (query.BatchId == Guid.Empty)
            return CatalogImportErrors.BatchNotFound(query.BatchId);

        if (!query.ExpectedVersion.HasValue)
            return GeneralErrors.ValueIsRequired(
                nameof(query.ExpectedVersion));

        if (query.RowIds is null ||
            query.RowIds.Count is < 1 or > MaximumRows ||
            query.RowIds.Any(id => id == Guid.Empty) ||
            query.RowIds.Distinct().Count() != query.RowIds.Count)
        {
            return new DomainError(
                "catalog.import.explanations.invalid_rows",
                "Укажите от 1 до 25 различных идентификаторов строк.");
        }

        var user = await users
            .GetByIdAsync(query.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return CatalogImportErrors.CurrentUserNotFound();

        var batch = await batches
            .GetByIdAsync(query.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
            return CatalogImportErrors.BatchNotFound(query.BatchId);

        // Те же права чтения, что у списка строк.
        if (batch.CreatedByUserId != user.Id &&
            !user.CanReviewCatalogImports())
        {
            return CatalogImportErrors.UserCannotAccessBatch();
        }

        var version = batch.Version;

        if (version != query.ExpectedVersion.Value)
            return CatalogImportErrors.BatchConcurrencyConflict();

        var rows = await batches
            .GetRowsByIdsAsync(
                batch.Id,
                query.RowIds,
                cancellationToken)
            .ConfigureAwait(false);

        if (rows.Count != query.RowIds.Count)
        {
            return new DomainError(
                "catalog.import.row.not_found",
                "Одна или несколько строк отсутствуют в этом пакете. " +
                "Обновите список.");
        }

        string[] allowedCodes = [];

        if (batch.ProductTypeId.HasValue)
        {
            var productType = await metadata
                .GetProductTypeByIdAsync(
                    batch.ProductTypeId.Value,
                    cancellationToken)
                .ConfigureAwait(false);

            if (productType is null)
            {
                return CatalogImportErrors.ProductTypeNotFound(
                    batch.ProductTypeId.Value);
            }

            var definitionIds = productType.Characteristics
                .Select(item => item.CharacteristicDefinitionId)
                .Distinct()
                .ToArray();

            var definitions = await metadata
                .GetCharacteristicDefinitionsByIdsAsync(
                    definitionIds,
                    cancellationToken)
                .ConfigureAwait(false);

            allowedCodes = definitions
                .Where(definition =>
                    productType.AllowsCharacteristic(definition.Id))
                .GroupBy(
                    definition =>
                        CatalogRecognitionTextNormalizer.NormalizeCode(
                            definition.Code),
                    StringComparer.Ordinal)
                .Where(group => group.Count() == 1)
                .Select(group => group.Key)
                .ToArray();
        }

        var manufacturerIndex = await manufacturers
            .LoadIndexAsync(cancellationToken)
            .ConfigureAwait(false);

        var typeIndex = await productTypes
            .LoadIndexAsync(cancellationToken)
            .ConfigureAwait(false);

        var items =
            new List<CatalogImportRowExplanationResult>(rows.Count);

        foreach (var row in rows.OrderBy(item => item.RowNumber))
        {
            cancellationToken.ThrowIfCancellationRequested();

            CatalogImportNormalizedRowData? data;

            try
            {
                data = JsonSerializer
                    .Deserialize<CatalogImportNormalizedRowData>(
                        row.NormalizedDataJson,
                        JsonOptions);
            }
            catch (JsonException)
            {
                return CatalogImportErrors.InvalidImportJson(
                    "catalogImportRow");
            }

            if (data is null)
            {
                return CatalogImportErrors.InvalidImportJson(
                    "catalogImportRow");
            }

            var name = data.Name;

            CatalogImportRowExplanationStatus status;

            if (string.IsNullOrWhiteSpace(name))
            {
                status = CatalogImportRowExplanationStatus.NameMissing;
            }
            else if (name.Length > MaximumNameLength)
            {
                status = CatalogImportRowExplanationStatus.NameTooLong;
            }
            else if (!batch.ProductTypeId.HasValue)
            {
                status = CatalogImportRowExplanationStatus.ProductTypeRequired;
            }
            else
            {
                status = CatalogImportRowExplanationStatus.Ready;
            }

            if (status != CatalogImportRowExplanationStatus.Ready)
            {
                items.Add(new(
                    row.Id,
                    row.RowNumber,
                    name,
                    status,
                    false,
                    null));

                continue;
            }

            try
            {
                var manufacturer =
                    manufacturerIndex.RecognizeInText(name!);

                var type = typeIndex.Suggest(
                    name!,
                    cancellationToken);

                var evidence = manufacturer.Candidates
                    .Select(candidate =>
                        new CatalogProductNameEvidenceSpan(
                            CatalogProductNameEvidenceKind.Manufacturer,
                            "MANUFACTURER",
                            candidate.ManufacturerName,
                            candidate.RawValue,
                            candidate.Source.ToString(),
                            candidate.Confidence,
                            0,
                            candidate.StartIndex,
                            candidate.Length))
                    .ToList();

                evidence.AddRange(
                    type.Candidates.SelectMany(candidate =>
                        candidate.Evidence.Select(span =>
                            new CatalogProductNameEvidenceSpan(
                                CatalogProductNameEvidenceKind.ProductType,
                                candidate.ProductTypeCode,
                                candidate.ProductTypeName,
                                span.RawValue,
                                span.Source,
                                candidate.Confidence,
                                span.Priority,
                                span.StartIndex,
                                span.Length))));

                var hasConflicts =
                    manufacturer.IsConflict || type.IsConflict;

                if (allowedCodes.Length > 0)
                {
                    var recognized = await recognition
                        .RecognizeAsync(
                            new CatalogProductNameRecognitionRequest(
                                name!,
                                batch.ProductTypeId,
                                allowedCodes),
                            cancellationToken)
                        .ConfigureAwait(false);

                    hasConflicts |= recognized.Conflicts.Count > 0;

                    foreach (var candidate in recognized.Candidates)
                    {
                        var mapped = CatalogImportNameEvidenceMapper.Map(
                            name!,
                            candidate);

                        if (mapped is null)
                        {
                            status =
                                CatalogImportRowExplanationStatus.InvalidEvidence;

                            break;
                        }

                        evidence.Add(mapped);
                    }
                }

                // Проверяем координаты до вычисления покрытия.
                if (evidence.Any(span =>
                    span.StartIndex < 0 ||
                    span.Length <= 0 ||
                    span.StartIndex > name!.Length - span.Length ||
                    !string.Equals(
                        name.Substring(span.StartIndex, span.Length),
                        span.RawValue,
                        StringComparison.Ordinal)))
                {
                    status =
                        CatalogImportRowExplanationStatus.InvalidEvidence;
                }

                var explanation =
                    status == CatalogImportRowExplanationStatus.Ready
                        ? coverage.Explain(
                            name!,
                            evidence.Distinct().ToArray())
                        : null;

                items.Add(new(
                    row.Id,
                    row.RowNumber,
                    name,
                    status,
                    hasConflicts,
                    explanation));
            }
            catch (RegexMatchTimeoutException)
            {
                items.Add(new(
                    row.Id,
                    row.RowNumber,
                    name,
                    CatalogImportRowExplanationStatus.RecognitionTimedOut,
                    false,
                    null));
            }
        }

        // Повторно читаем версию именно из базы.
        var currentVersion = await batches
            .GetVersionAsync(batch.Id, cancellationToken)
            .ConfigureAwait(false);

        if (currentVersion != version)
            return CatalogImportErrors.BatchConcurrencyConflict();

        return new GetCatalogImportRowExplanationsResult(
            batch.Id,
            version,
            DateTimeOffset.UtcNow,
            items);
    }
}