using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.CreateProduct;

public sealed class CreateProductCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICatalogProductMetadataRepository _metadataRepository;
    private readonly ProductAuditSnapshotBuilder _snapshotBuilder;
    private readonly IProductAuditRepository _auditRepository;

    public CreateProductCommandHandler(
        IUserRepository userRepository,
        IProductRepository productRepository,
        ICatalogProductMetadataRepository metadataRepository,
        ProductAuditSnapshotBuilder snapshotBuilder,
        IProductAuditRepository auditRepository)
    {
        _userRepository = userRepository;
        _productRepository = productRepository;
        _metadataRepository = metadataRepository;
        _snapshotBuilder = snapshotBuilder;
        _auditRepository = auditRepository;
    }

    public async Task<Result<CreateProductResult, DomainError>> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currentUser = await _userRepository
            .GetByIdAsync(command.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null || !currentUser.IsActive)
        {
            return CatalogErrors.CurrentUserIsRequired();
        }

        var productType = await _metadataRepository
            .GetProductTypeByIdAsync(command.ProductTypeId, cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            return CatalogErrors.ProductTypeNotFound(
                command.ProductTypeId.ToString());
        }

        var manufacturer = await _metadataRepository
            .GetManufacturerByIdAsync(command.ManufacturerId, cancellationToken)
            .ConfigureAwait(false);

        if (manufacturer is null)
        {
            return CatalogErrors.ManufacturerNotFound(command.ManufacturerId);
        }

        var priceResult = Money.Create(command.PriceAmount);

        if (priceResult.IsFailure)
        {
            return priceResult.Error;
        }

        var stockResult = StockQuantity.Create(command.StockQuantity);

        if (stockResult.IsFailure)
        {
            return stockResult.Error;
        }

        var productResult = Product.Create(
            command.Article,
            command.Name,
            productType.Id,
            manufacturer.Id,
            priceResult.Value,
            stockResult.Value);

        if (productResult.IsFailure)
        {
            return productResult.Error;
        }

        var product = productResult.Value;

        var repeatedCharacteristicCode = command.Characteristics
            .Where(item => !string.IsNullOrWhiteSpace(item.Code))
            .GroupBy(item => item.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (repeatedCharacteristicCode is not null)
        {
            return GeneralErrors.ValueIsInvalid(nameof(command.Characteristics));
        }

        foreach (var characteristic in command.Characteristics)
        {
            if (string.IsNullOrWhiteSpace(characteristic.Code)
                || string.IsNullOrWhiteSpace(characteristic.Value))
            {
                return GeneralErrors.ValueIsInvalid(nameof(command.Characteristics));
            }

            var definition = await _metadataRepository
                .GetCharacteristicDefinitionByCodeAsync(
                    characteristic.Code,
                    cancellationToken)
                .ConfigureAwait(false);

            if (definition is null)
            {
                return CatalogErrors.CharacteristicDefinitionNotFound(
                    characteristic.Code);
            }

            var valueResult = ProductCharacteristicValueFactory.Create(
                definition.Code,
                definition.DataType,
                characteristic.Value);

            if (valueResult.IsFailure)
            {
                return valueResult.Error;
            }

            var setResult = product.SetCharacteristic(
                productType,
                definition,
                valueResult.Value);

            if (setResult.IsFailure)
            {
                return setResult.Error;
            }
        }

        var requiredCharacteristicsResult =
            product.ValidateRequiredCharacteristics(productType);

        if (requiredCharacteristicsResult.IsFailure)
        {
            return requiredCharacteristicsResult.Error;
        }

        var productAlreadyExists = await _productRepository
            .ExistsByArticleAndManufacturerAsync(
                product.Article.Value,
                product.ManufacturerId,
                cancellationToken)
            .ConfigureAwait(false);

        if (productAlreadyExists)
        {
            return CatalogErrors.ProductAlreadyExists(
                product.Article.Value,
                product.ManufacturerId);
        }

        var snapshotResult = await _snapshotBuilder
            .BuildAsync(product, cancellationToken)
            .ConfigureAwait(false);

        if (snapshotResult.IsFailure)
        {
            return snapshotResult.Error;
        }

        var auditResult = ProductAuditEntry.Create(
            product.Id,
            command.CurrentUserId,
            ProductAuditOperation.ProductCreated,
            ProductAuditSource.Manual,
            sourceId: null,
            beforeJson: null,
            ProductAuditSnapshotSerializer.Serialize(snapshotResult.Value));

        if (auditResult.IsFailure)
        {
            return auditResult.Error;
        }

        _productRepository.Add(product);
        _auditRepository.Add(auditResult.Value);

        await _productRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return new CreateProductResult(
            product.Id,
            product.Article.Value,
            product.Name.Value,
            product.ProductTypeId,
            product.ManufacturerId,
            product.Price.Amount,
            product.StockQuantity.Value);
    }
}
