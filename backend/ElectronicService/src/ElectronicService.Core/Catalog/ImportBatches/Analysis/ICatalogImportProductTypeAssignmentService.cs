using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public interface ICatalogImportProductTypeAssignmentService
{
    Task<Result<CatalogImportWorkbookAnalysis, DomainError>>
        AssignAsync(
            CatalogImportWorkbookAnalysis analysis,
            ProductType? batchProductType,
            CancellationToken cancellationToken = default);
}