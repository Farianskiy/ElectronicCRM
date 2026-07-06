namespace ElectronicService.Contracts.Catalog.Products.Management;

public sealed record UpdateProductPriceRequest(
    decimal Amount,
    string Currency);