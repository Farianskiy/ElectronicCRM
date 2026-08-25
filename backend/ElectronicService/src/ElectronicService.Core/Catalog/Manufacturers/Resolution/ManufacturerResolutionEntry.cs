namespace ElectronicService.Core.Catalog.Manufacturers.Resolution;

public sealed record ManufacturerResolutionEntry(
    Guid ManufacturerId,
    string ManufacturerName,
    string NormalizedInputName,
    ManufacturerResolutionSource Source,
    Guid? ManufacturerAliasId);