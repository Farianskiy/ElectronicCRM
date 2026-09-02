using System.Text.Json.Serialization;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CatalogImportCharacteristicValueSource
{
    None = 0,

    Excel = 1,

    Recognition = 2,

    Manual = 3
}