using ElectronicService.Core.Catalog.Recognition.Models;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogProductNameRecognitionService
{
    Task<CatalogProductNameRecognitionResult> RecognizeAsync(CatalogProductNameRecognitionRequest request, CancellationToken cancellationToken = default);
}