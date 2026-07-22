using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products
    .PreviewProductTypeMigration;

public sealed class
    PreviewProductTypeMigrationQueryHandler
{
    private readonly ProductTypeMigrationPlanner
        _planner;

    public PreviewProductTypeMigrationQueryHandler(
        ProductTypeMigrationPlanner planner)
    {
        _planner = planner;
    }

    public async Task<Result<
        ProductTypeMigrationPreviewResult,
        DomainError>> Handle(
            PreviewProductTypeMigrationQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var planResult = await _planner
            .BuildAsync(
                query.ProductId,
                query.TargetProductTypeId,
                cancellationToken)
            .ConfigureAwait(false);

        if (planResult.IsFailure)
        {
            return Result.Failure<
                ProductTypeMigrationPreviewResult,
                DomainError>(
                    planResult.Error);
        }

        return Result.Success<
            ProductTypeMigrationPreviewResult,
            DomainError>(
                planResult.Value.Preview);
    }
}