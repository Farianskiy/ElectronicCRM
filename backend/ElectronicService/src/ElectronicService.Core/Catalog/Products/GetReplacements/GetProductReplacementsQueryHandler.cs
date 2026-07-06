using ElectronicService.Core.Catalog.Products.Abstractions;

namespace ElectronicService.Core.Catalog.Products.GetReplacements;

public sealed class GetProductReplacementsQueryHandler
{
    private readonly ICatalogProductReplacementsReader _replacementsReader;

    public GetProductReplacementsQueryHandler(
        ICatalogProductReplacementsReader replacementsReader)
    {
        _replacementsReader = replacementsReader;
    }

    public Task<CatalogProductReplacementsResult?> Handle(
        GetProductReplacementsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _replacementsReader.GetReplacementsAsync(
            query,
            cancellationToken);
    }
}