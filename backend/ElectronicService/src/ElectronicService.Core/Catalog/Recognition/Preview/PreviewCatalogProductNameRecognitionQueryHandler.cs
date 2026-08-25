using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Core.Catalog.ProductTypes.GetCharacteristicSchema;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Models;

namespace ElectronicService.Core.Catalog.Recognition.Preview;

public sealed class PreviewCatalogProductNameRecognitionQueryHandler
{
    private readonly ICatalogProductTypeSchemaReader _productTypeSchemaReader;
    private readonly ICatalogCharacteristicRecognitionProfileReader _recognitionProfileReader;
    private readonly ICatalogProductNameRecognitionService _recognitionService;

    public PreviewCatalogProductNameRecognitionQueryHandler(
        ICatalogProductTypeSchemaReader productTypeSchemaReader,
        ICatalogCharacteristicRecognitionProfileReader recognitionProfileReader,
        ICatalogProductNameRecognitionService recognitionService)
    {
        ArgumentNullException.ThrowIfNull(productTypeSchemaReader);
        ArgumentNullException.ThrowIfNull(recognitionProfileReader);
        ArgumentNullException.ThrowIfNull(recognitionService);

        _productTypeSchemaReader = productTypeSchemaReader;
        _recognitionProfileReader = recognitionProfileReader;
        _recognitionService = recognitionService;
    }

    public async Task<CatalogProductNameRecognitionPreviewResult?> Handle(
        PreviewCatalogProductNameRecognitionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.ProductName);

        CatalogProductTypeCharacteristicSchemaResult? productTypeSchema = null;

        if (!string.IsNullOrWhiteSpace(query.ProductTypeCode))
        {
            productTypeSchema = await _productTypeSchemaReader
                .GetByCodeAsync(query.ProductTypeCode.Trim(), cancellationToken)
                .ConfigureAwait(false);

            if (productTypeSchema is null)
            {
                return null;
            }
        }

        IReadOnlyCollection<string>? allowedCharacteristicCodes = productTypeSchema?
            .Characteristics
            .Select(characteristic => characteristic.Code)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        IReadOnlyCollection<CatalogCharacteristicRecognitionProfileResult> recognitionProfiles = [];

        if (productTypeSchema is not null)
        {
            recognitionProfiles = await _recognitionProfileReader
                .GetProfilesAsync(productTypeSchema.ProductTypeId, cancellationToken)
                .ConfigureAwait(false);
        }

        var recognitionRequest = new CatalogProductNameRecognitionRequest(
            query.ProductName,
            productTypeSchema?.ProductTypeId,
            allowedCharacteristicCodes);

        var recognitionResult = await _recognitionService
            .RecognizeAsync(recognitionRequest, cancellationToken)
            .ConfigureAwait(false);

        return new CatalogProductNameRecognitionPreviewResult(
            productTypeSchema?.ProductTypeId,
            productTypeSchema?.ProductTypeCode,
            productTypeSchema?.ProductTypeName,
            allowedCharacteristicCodes,
            recognitionProfiles,
            recognitionResult);
    }
}