using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record TrainingExampleFilter(Guid? ManufacturerId = null, Guid? ProductTypeId = null,
    Guid? CharacteristicDefinitionId = null, string Status = "active", int Page = 1);

public sealed record TrainingExampleItem(Guid Id, Guid SourceFeedbackId, Guid ManufacturerId,
    Guid ProductTypeId, Guid CharacteristicDefinitionId, string ProductName, string RawValue,
    string NormalizedValue, int SpanStart, int SpanLength, DateTime ConfirmedAtUtc,
    DateTime? RevokedAtUtc, string? RevocationReason, Guid? ImportBatchId,
    string ManufacturerName, string ProductTypeName, string CharacteristicName,
    DateTime? SourceExcludedAtUtc, string? SourceExclusionReason, bool SourceAvailable, bool IsEvaluationOnly);

public sealed record TrainingExamplePage(IReadOnlyList<TrainingExampleItem> Items, int Page, bool HasMore);

public sealed record CatalogImportTrainingSummaryGroup(
    Guid ManufacturerId,
    string ManufacturerName,
    Guid ProductTypeId,
    string ProductTypeName,
    Guid CharacteristicDefinitionId,
    string CharacteristicName,
    int ActiveExamplesCount,
    int DistinctValuesCount);

public sealed record CatalogImportTrainingSummary(
    Guid BatchId,
    int ActiveExamplesCount,
    int EvaluationExamplesCount,
    int RevokedExamplesCount,
    IReadOnlyList<CatalogImportTrainingSummaryGroup> Groups);

public sealed record ConfirmedExampleExportMetadata(DateTime SelectionAsOfUtc, long Count);

public interface ICatalogTrainingExampleManagement
{
    Task<Result<TrainingExamplePage, DomainError>> ListAsync(TrainingExampleFilter filter, CancellationToken cancellationToken);
    Task<Result<CatalogImportTrainingSummary, DomainError>> GetImportSummaryAsync(Guid batchId, CancellationToken cancellationToken);
    Task<Result<TrainingExampleItem, DomainError>> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<UnitResult<DomainError>> SetPurposeAsync(Guid id, bool evaluationOnly, CancellationToken cancellationToken);
    Task<UnitResult<DomainError>> RevokeAsync(Guid id, string reason, CancellationToken cancellationToken);
    Task<Result<ConfirmedExampleExportMetadata, DomainError>> ExportAsync(TrainingExampleFilter filter, Stream destination, CancellationToken cancellationToken);
}
