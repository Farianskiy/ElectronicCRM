using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;

namespace ElectronicService.Core.Catalog.PriceCalculations.ExportCatalogPriceCalculation;

public interface ICatalogPriceCalculationWorkbookExporter
{
    byte[] Export(CatalogPriceCalculationDetails calculation, DateTime generatedAtUtc);
}