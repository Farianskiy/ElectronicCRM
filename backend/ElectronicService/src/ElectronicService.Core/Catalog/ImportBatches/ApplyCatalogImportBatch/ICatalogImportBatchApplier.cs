using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.ApplyCatalogImportBatch;

public interface ICatalogImportBatchApplier
{
    Task<Result<CatalogImportApplyExecutionResult, DomainError>> ApplyAsync(
        CatalogImportBatch batch,
        Guid appliedByUserId,
        CancellationToken cancellationToken = default);
}