using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;

namespace ElectronicService.Core.Catalog.ImportBatches.Abstractions;

public interface ICatalogImportBatchRepository
{
    void Add(CatalogImportBatch batch);

    void Remove(CatalogImportBatch batch);

    Task<CatalogImportBatch?> GetByIdWithFileAsync(Guid batchId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogImportColumn>> GetColumnsForAnalysisAsync(Guid batchId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogImportColumn>> GetColumnsForUpdateAsync(Guid batchId, CancellationToken cancellationToken = default);

    Task<CatalogImportBatch?> GetByIdAsync(Guid batchId, CancellationToken cancellationToken = default);

    Task<CatalogImportRow?> GetRowByIdAsync(Guid batchId, Guid rowId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogImportRow>> GetRowsByIdsAsync(Guid batchId, IReadOnlyCollection<Guid> rowIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogImportRow>> GetRowsAsync(Guid batchId, CatalogImportRowStatus? status, string? search, string? issueCode, CatalogImportRowProblemKind? problemKind, string? manufacturerGroupKey, int skip, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogImportRow>> GetRowsByKnowledgeStateAsync(Guid batchId, CatalogImportRowStatus? status, string? search, string? issueCode, CatalogImportRowProblemKind? problemKind, string? manufacturerGroupKey, CatalogImportRowKnowledgeState knowledgeState, int skip, int take, CancellationToken cancellationToken = default);

    Task<int> CountRowsAsync(Guid batchId, CatalogImportRowStatus? status, string? search, string? issueCode, CatalogImportRowProblemKind? problemKind, string? manufacturerGroupKey, CancellationToken cancellationToken = default);

    Task<int> CountRowsByKnowledgeStateAsync(Guid batchId, CatalogImportRowStatus? status, string? search, string? issueCode, CatalogImportRowProblemKind? problemKind, string? manufacturerGroupKey, CatalogImportRowKnowledgeState knowledgeState, CancellationToken cancellationToken = default);

    Task<CatalogImportRowProblemsSummary> GetRowProblemCodesAsync(Guid batchId, CatalogImportRowStatus? status, string? search, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogImportManufacturerGroupSummary>> GetManufacturerGroupsAsync(Guid batchId, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogImportBatch>> GetReviewQueueAsync(CatalogImportBatchStatus? status, int skip, int take, CancellationToken cancellationToken = default);

    Task<int> CountReviewQueueAsync(CatalogImportBatchStatus? status, CancellationToken cancellationToken = default);

    Task<bool> ReplaceAnalysisAsync(CatalogImportBatch batch, IReadOnlyCollection<CatalogImportColumn> columns, IReadOnlyCollection<CatalogImportRow> rows, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogImportBatch>> GetByCreatorAsync(Guid createdByUserId, CatalogImportBatchStatus? status, int skip, int take, CancellationToken cancellationToken = default);

    Task<int> CountByCreatorAsync(Guid createdByUserId, CatalogImportBatchStatus? status, CancellationToken cancellationToken = default);

    Task<uint?> GetVersionAsync(Guid batchId, CancellationToken cancellationToken = default);

    Task<CatalogImportApplicationPlan> GetApplicationPlanAsync(
        Guid batchId,
        CancellationToken cancellationToken = default);

}

public sealed record CatalogImportApplicationPlan(
    int CreateProductsCount,
    int UpdateProductsCount);

public sealed record CatalogImportRowProblemsSummary(
    int TotalRowsCount,
    int ErrorRowsCount,
    int WarningRowsCount,
    int StrictRowsCount,
    int CatalogAnalogyRowsCount,
    int AwaitingConfirmationRowsCount,
    int ConflictRowsCount,
    int RequiredMissingRowsCount,
    IReadOnlyCollection<CatalogImportRowProblemCodeSummary> Items);

public sealed record CatalogImportRowProblemCodeSummary(
    string Code,
    int RowsCount,
    int ErrorRowsCount,
    int WarningRowsCount);

public sealed record CatalogImportManufacturerGroupSummary(
    string GroupKey,
    string SourceValue,
    string ResolutionSource,
    string? ResolvedManufacturerName,
    int ExactNameRowsCount,
    int ApprovedAliasRowsCount,
    int IgnoredNoiseRowsCount,
    int UnresolvedRowsCount,
    int ManualRowsCount,
    int RowsCount);
