using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ProductTypes.ChangeKind;

public sealed class ChangeProductTypeKindCommandHandler
{
    private readonly ProductTypeKindChangePlanner _planner;
    private readonly IProductTypeKindManagementRepository _repository;

    public ChangeProductTypeKindCommandHandler(
        ProductTypeKindChangePlanner planner,
        IProductTypeKindManagementRepository repository)
    {
        _planner = planner;
        _repository = repository;
    }

    public async Task<Result<ProductTypeKindChangePreview, DomainError>> Handle(
        ChangeProductTypeKindCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var previewResult = await _planner
            .BuildAsync(
                command.ProductTypeId,
                command.TargetKind,
                cancellationToken)
            .ConfigureAwait(false);

        if (previewResult.IsFailure)
        {
            return previewResult;
        }

        if (!previewResult.Value.CanChange)
        {
            return Result.Failure<ProductTypeKindChangePreview, DomainError>(
                CatalogErrors.ProductTypeKindChangeIsBlocked());
        }

        var productType = await _repository
            .GetByIdAsync(command.ProductTypeId, cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            return Result.Failure<ProductTypeKindChangePreview, DomainError>(
                CatalogErrors.ProductTypeNotFound(command.ProductTypeId.ToString()));
        }

        var changeResult = productType.ChangeKind(command.TargetKind);

        if (changeResult.IsFailure)
        {
            return Result.Failure<ProductTypeKindChangePreview, DomainError>(
                changeResult.Error);
        }

        await _repository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return previewResult;
    }
}
