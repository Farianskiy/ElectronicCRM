using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Catalog.ImportBatches.ApplyCatalogImportBatch;

public interface ICatalogImportBatchApplier
{
    Task<Result<CatalogImportApplyExecutionResult, DomainError>> ApplyAsync(
        CatalogImportBatch batch,
        Guid appliedByUserId,
        UserType appliedByUserType,
        CancellationToken cancellationToken = default);
}