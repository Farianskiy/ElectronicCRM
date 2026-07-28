using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public interface ICatalogImportRowValidator
{
    CatalogImportRowValidationResult Validate(
        CatalogImportNormalizedRowData data,
        ProductType productType,
        IReadOnlyCollection<CharacteristicDefinition> characteristicDefinitions);
}