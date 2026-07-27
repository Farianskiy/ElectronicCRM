using System.Text.Json;
using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog
    .ImportBatches.Preview;

public sealed record
    CatalogImportBatchDetailsResult(
        Guid BatchId,
        Guid CreatedByUserId,
        Guid? ProductTypeId,
        string OriginalFileName,
        string ContentType,
        long FileSizeBytes,
        string FileSha256,
        CatalogImportBatchStatus Status,
        int RowsCount,
        int ValidRowsCount,
        int ErrorRowsCount,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc,
        DateTime? SubmittedAtUtc,
        DateTime? ReviewedAtUtc,
        DateTime? AppliedAtUtc,
        DateTime? RejectedAtUtc,
        string? RejectionReason,
        string? FailureReason,
        uint Version);

public sealed record CatalogImportColumnResult(
    Guid ColumnId,
    int SourceColumnNumber,
    string SourceHeader,
    string NormalizedSourceHeader,
    CatalogImportColumnTargetKind TargetKind,
    Guid? CharacteristicDefinitionId,
    string? CharacteristicCode,
    string? CharacteristicName,
    string? CharacteristicDataType,
    string? CharacteristicUnit,
    decimal Confidence,
    bool IsConfirmed,
    bool IsMapped);

public sealed record CatalogImportRowResult(
    Guid RowId,
    int RowNumber,
    CatalogImportRowStatus Status,
    JsonElement RawData,
    JsonElement NormalizedData,
    JsonElement Issues,
    JsonElement Warnings);

public sealed record CatalogImportRowsPageResult(
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyCollection<
        CatalogImportRowResult> Items);