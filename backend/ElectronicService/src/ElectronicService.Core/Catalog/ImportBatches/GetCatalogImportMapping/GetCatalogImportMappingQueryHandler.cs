using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportMapping;

public sealed class GetCatalogImportMappingQueryHandler
{
    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly IUserRepository _userRepository;

    public GetCatalogImportMappingQueryHandler(
        ICatalogImportBatchRepository importBatchRepository,
        IUserRepository userRepository)
    {
        _importBatchRepository = importBatchRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<GetCatalogImportMappingResult, DomainError>> Handle(
        GetCatalogImportMappingQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<GetCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (query.BatchId == Guid.Empty)
        {
            return Result.Failure<GetCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.BatchNotFound(query.BatchId));
        }

        var currentUser = await _userRepository
            .GetByIdAsync(query.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<GetCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanViewOwnCatalogImports())
        {
            return Result.Failure<GetCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.UserCannotViewOwnCatalogImports());
        }

        var batch = await _importBatchRepository
            .GetByIdAsync(query.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<GetCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.BatchNotFound(query.BatchId));
        }

        if (batch.CreatedByUserId != currentUser.Id)
        {
            return Result.Failure<GetCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.UserCannotAccessBatch());
        }

        var columns = await _importBatchRepository
            .GetColumnsForAnalysisAsync(batch.Id, cancellationToken)
            .ConfigureAwait(false);

        var columnResults = columns
            .OrderBy(column => column.SourceColumnNumber)
            .Select(column => new CatalogImportColumnMappingResult(
                column.Id,
                column.SourceColumnNumber,
                column.SourceHeader,
                column.TargetKind,
                column.CharacteristicDefinitionId,
                column.Confidence,
                column.IsConfirmed))
            .ToArray();

        var canEdit =
            currentUser.CanEditCatalogImport()
            && batch.IsEditable;

        var result = new GetCatalogImportMappingResult(
            batch.Id,
            batch.Status,
            batch.ProductTypeId,
            columnResults,
            batch.Version,
            canEdit);

        return Result.Success<GetCatalogImportMappingResult, DomainError>(
            result);
    }
}