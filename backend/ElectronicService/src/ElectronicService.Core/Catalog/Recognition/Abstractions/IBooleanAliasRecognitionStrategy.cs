using ElectronicService.Core.Catalog.Recognition.Configuration;
using ElectronicService.Core.Catalog.Recognition.Models;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface IBooleanAliasRecognitionStrategy : ICatalogCharacteristicRecognitionStrategy
{
    IReadOnlyCollection<CatalogRecognizedCharacteristic> Recognize(string productName, BooleanAliasRecognitionSettings settings);
}