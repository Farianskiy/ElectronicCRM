using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ProductTypes.Lifecycle;

public sealed class PreviewProductTypeArchiveQueryHandler
{
    private readonly ProductTypeArchivePlanner _planner;

    public PreviewProductTypeArchiveQueryHandler(ProductTypeArchivePlanner planner)
    {
        _planner = planner;
    }

    public Task<Result<ProductTypeArchivePreview, DomainError>> Handle(
        PreviewProductTypeArchiveQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _planner.BuildAsync(query.ProductTypeId, cancellationToken);
    }
}
