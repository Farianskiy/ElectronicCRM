using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogCharacteristicRecognitionStrategy
{
    string CharacteristicCode { get; }

    CatalogCharacteristicRecognitionStrategyKind StrategyKind { get; }

    int Priority { get; }

    IReadOnlyCollection<CatalogRecognizedCharacteristic> Recognize(string productName);
}