using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.BulkUpdateProducts;

public sealed class BulkUpdateCatalogProductsCommandHandler
{
    public const int MaximumRowsCount = 100;

    private readonly IProductRepository _productRepository;
    private readonly ICatalogProductMetadataRepository _metadataRepository;
    private readonly ProductAuditRecorder _auditRecorder;

    public BulkUpdateCatalogProductsCommandHandler(
        IProductRepository productRepository,
        ICatalogProductMetadataRepository metadataRepository,
        ProductAuditRecorder auditRecorder)
    {
        _productRepository = productRepository;
        _metadataRepository = metadataRepository;
        _auditRecorder = auditRecorder;
    }

    public async Task<Result<BulkUpdateCatalogProductsResult, DomainError>> Handle(
        BulkUpdateCatalogProductsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Rows);

        if (command.ChangedByUserId == Guid.Empty)
        {
            return CatalogErrors.CurrentUserIsRequired();
        }

        if (command.Rows.Count == 0)
        {
            return CatalogErrors.BulkProductsRequired();
        }

        if (command.Rows.Count > MaximumRowsCount)
        {
            return CatalogErrors.BulkProductsLimitExceeded(MaximumRowsCount);
        }

        var duplicateProductId = command.Rows
            .GroupBy(row => row.ProductId)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateProductId.HasValue)
        {
            return CatalogErrors.DuplicateBulkProduct(duplicateProductId.Value);
        }

        foreach (var row in command.Rows)
        {
            if (row.ProductId == Guid.Empty)
            {
                return GeneralErrors.ValueIsInvalid(nameof(row.ProductId));
            }

            if (!row.HasChanges)
            {
                return CatalogErrors.BulkProductChangesRequired(row.ProductId);
            }

            if (row.ChangesGeneralInformation && !command.CanEditProducts)
            {
                return CatalogErrors.BulkProductPermissionDenied("основная информация");
            }

            if (row.ChangesPrice && !command.CanManagePrices)
            {
                return CatalogErrors.BulkProductPermissionDenied("цена");
            }

            if (row.ChangesStock && !command.CanManageStock)
            {
                return CatalogErrors.BulkProductPermissionDenied("остаток");
            }
        }

        var productIds = command.Rows.Select(row => row.ProductId).ToArray();
        var products = await _productRepository
            .GetByIdsWithDetailsAsync(productIds, cancellationToken)
            .ConfigureAwait(false);

        var productsById = products.ToDictionary(product => product.Id);

        var missingProductId = productIds.FirstOrDefault(
            productId => !productsById.ContainsKey(productId));

        if (missingProductId != Guid.Empty)
        {
            return CatalogErrors.ProductNotFound(missingProductId.ToString());
        }

        var manufacturers = await _metadataRepository
            .GetManufacturersAsync(cancellationToken)
            .ConfigureAwait(false);
        var manufacturerIds = manufacturers.Select(value => value.Id).ToHashSet();

        var preparedRows = new List<PreparedRow>(command.Rows.Count);

        foreach (var row in command.Rows)
        {
            var product = productsById[row.ProductId];
            var name = row.Name ?? product.Name.Value;
            var article = row.Article ?? product.Article.Value;
            var manufacturerId = row.ManufacturerId ?? product.ManufacturerId;
            var amount = row.PriceAmount ?? product.Price.Amount;
            var currency = row.PriceCurrency ?? product.Price.Currency;
            var quantity = row.StockQuantity ?? product.StockQuantity.Value;

            var nameResult = ProductName.Create(name);
            if (nameResult.IsFailure) return nameResult.Error;

            var articleResult = ProductArticle.Create(article);
            if (articleResult.IsFailure) return articleResult.Error;

            if (!manufacturerIds.Contains(manufacturerId))
            {
                return CatalogErrors.ManufacturerNotFound(manufacturerId);
            }

            var priceResult = Money.Create(amount, currency);
            if (priceResult.IsFailure) return priceResult.Error;

            var stockResult = StockQuantity.Create(quantity);
            if (stockResult.IsFailure) return stockResult.Error;

            preparedRows.Add(new PreparedRow(
                row,
                product,
                nameResult.Value.Value,
                articleResult.Value.Value,
                manufacturerId,
                priceResult.Value,
                stockResult.Value));
        }

        var preparedProducts = preparedRows
            .Select(prepared => prepared.Product)
            .ToArray();
        var beforeSnapshotsResult = await _auditRecorder
            .CaptureManyAsync(preparedProducts, cancellationToken)
            .ConfigureAwait(false);

        if (beforeSnapshotsResult.IsFailure)
        {
            return beforeSnapshotsResult.Error;
        }

        foreach (var prepared in preparedRows)
        {
            if (prepared.Request.ChangesGeneralInformation)
            {
                var updateResult = prepared.Product.UpdateGeneralInformation(
                    prepared.Name,
                    prepared.Article,
                    prepared.ManufacturerId);

                if (updateResult.IsFailure) return updateResult.Error;
            }

            if (prepared.Request.ChangesPrice)
            {
                var priceResult = prepared.Product.ChangePrice(prepared.Price);
                if (priceResult.IsFailure) return priceResult.Error;
            }

            if (prepared.Request.ChangesStock)
            {
                var stockResult = prepared.Product.ChangeStockQuantity(prepared.Stock);
                if (stockResult.IsFailure) return stockResult.Error;
            }
        }

        var afterSnapshotsResult = await _auditRecorder
            .CaptureManyAsync(preparedProducts, cancellationToken)
            .ConfigureAwait(false);

        if (afterSnapshotsResult.IsFailure)
        {
            return afterSnapshotsResult.Error;
        }

        var updatedProductIds = new List<Guid>(preparedRows.Count);

        foreach (var product in preparedProducts)
        {
            var beforeSnapshot = beforeSnapshotsResult.Value[product.Id];
            var afterSnapshot = afterSnapshotsResult.Value[product.Id];

            var auditResult = _auditRecorder
                .RecordPreparedManualChange(
                    product,
                    command.ChangedByUserId,
                    ProductAuditOperation.BulkUpdated,
                    beforeSnapshot,
                    afterSnapshot);

            if (auditResult.IsFailure)
            {
                return auditResult.Error;
            }

            if (auditResult.Value == ProductAuditRecordOutcome.Recorded)
            {
                updatedProductIds.Add(product.Id);
            }
        }

        if (updatedProductIds.Count > 0)
        {
            var saved = await _productRepository
                .TrySaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!saved)
            {
                return CatalogErrors.ProductConcurrencyConflict(updatedProductIds[0]);
            }
        }

        return new BulkUpdateCatalogProductsResult(updatedProductIds);
    }

    private sealed record PreparedRow(
        BulkUpdateCatalogProductItem Request,
        Product Product,
        string Name,
        string Article,
        Guid ManufacturerId,
        Money Price,
        StockQuantity Stock);
}
