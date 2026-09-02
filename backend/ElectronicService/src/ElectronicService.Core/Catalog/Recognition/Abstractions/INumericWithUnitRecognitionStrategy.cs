using ElectronicService.Core.Catalog.Recognition.Configuration;
using ElectronicService.Core.Catalog.Recognition.Models;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface INumericWithUnitRecognitionStrategy : ICatalogCharacteristicRecognitionStrategy
{
    IReadOnlyCollection<CatalogRecognizedCharacteristic> Recognize(
        string productName,
        NumericWithUnitRecognitionSettings settings);
}