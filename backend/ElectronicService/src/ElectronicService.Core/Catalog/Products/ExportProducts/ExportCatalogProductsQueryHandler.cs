using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.GetProducts;
using ElectronicService.Core.Catalog.Products.SearchProducts;

namespace ElectronicService.Core.Catalog.Products.ExportProducts;

public sealed class ExportCatalogProductsQueryHandler
{
    private const int ExportPageSize = 100;
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly ICatalogProductsReader _productsReader;
    private readonly ICatalogProductsWorkbookExporter _workbookExporter;

    public ExportCatalogProductsQueryHandler(
        ICatalogProductsReader productsReader,
        ICatalogProductsWorkbookExporter workbookExporter)
    {
        ArgumentNullException.ThrowIfNull(productsReader);
        ArgumentNullException.ThrowIfNull(workbookExporter);

        _productsReader = productsReader;
        _workbookExporter = workbookExporter;
    }

    public async Task<ExportCatalogProductsResult> Handle(
        SearchProductsQuery filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var products = new List<CatalogProductListItemResult>();
        var page = 1;
        var totalCount = 0;

        while (true)
        {
            var result = await _productsReader.SearchProductsAsync(
                    filter with
                    {
                        Page = page,
                        PageSize = ExportPageSize
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            totalCount = result.TotalCount;
            products.AddRange(result.Items);

            if (result.Items.Count == 0
                || page * ExportPageSize >= totalCount)
            {
                break;
            }

            page++;
        }

        var generatedAtUtc = DateTime.UtcNow;
        var content = await _workbookExporter.ExportAsync(
                products,
                generatedAtUtc,
                cancellationToken)
            .ConfigureAwait(false);

        return new ExportCatalogProductsResult(
            content,
            ExcelContentType,
            $"catalog-products-{generatedAtUtc:yyyy-MM-dd-HHmm}.xlsx",
            products.Count);
    }
}
