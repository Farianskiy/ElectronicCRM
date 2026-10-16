using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ProductTypes.ChangeKind;

public sealed class PreviewProductTypeKindChangeQueryHandler
{
    private readonly ProductTypeKindChangePlanner _planner;

    public PreviewProductTypeKindChangeQueryHandler(
        ProductTypeKindChangePlanner planner)
    {
        _planner = planner;
    }

    public Task<Result<ProductTypeKindChangePreview, DomainError>> Handle(
        PreviewProductTypeKindChangeQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _planner.BuildAsync(
            query.ProductTypeId,
            query.TargetKind,
            cancellationToken);
    }
}
