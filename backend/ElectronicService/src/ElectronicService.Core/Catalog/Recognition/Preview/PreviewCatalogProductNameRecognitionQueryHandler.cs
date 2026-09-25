using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Core.Catalog.ProductTypes.GetCharacteristicSchema;
using ElectronicService.Core.Catalog.Recognition.Effective;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Preview;

public sealed class PreviewCatalogProductNameRecognitionQueryHandler(
    ICatalogProductTypeSchemaReader productTypeSchemaReader,
    ICatalogProductMetadataRepository metadataRepository,
    ICatalogEffectiveRecognitionService recognitionService)
{
    public async Task<Result<CatalogProductNameRecognitionPreviewResult, DomainError>> Handle(
        PreviewCatalogProductNameRecognitionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.ProductName))
        {
            return new DomainError("recognition.name_required", "Укажите наименование товара.");
        }

        CatalogProductTypeCharacteristicSchemaResult? schema = null;
        IReadOnlyCollection<CharacteristicDefinition>? definitions = null;
        if (!string.IsNullOrWhiteSpace(query.ProductTypeCode))
        {
            schema = await productTypeSchemaReader.GetByCodeAsync(query.ProductTypeCode.Trim(), cancellationToken).ConfigureAwait(false);
            if (schema is null)
            {
                return new DomainError("recognition.product_type_not_found", "Тип товара не найден.");
            }

            definitions = await metadataRepository.GetCharacteristicDefinitionsByIdsAsync(
                schema.Characteristics.Select(item => item.DefinitionId).ToArray(), cancellationToken).ConfigureAwait(false);
        }

        string? manufacturerName = null;
        if (query.ManufacturerId.HasValue)
        {
            var manufacturer = await metadataRepository.GetManufacturerByIdAsync(query.ManufacturerId.Value, cancellationToken).ConfigureAwait(false);
            if (manufacturer is null)
            {
                return new DomainError("recognition.manufacturer_not_found", "Производитель не найден.");
            }

            manufacturerName = manufacturer.Name;
        }

        var context = await recognitionService.CreateRunAsync(cancellationToken).ConfigureAwait(false);
        var result = await recognitionService.RecognizeAsync(new CatalogEffectiveRecognitionRequest(
            query.ProductName, query.ManufacturerId, manufacturerName, schema?.ProductTypeId, definitions, context), cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var effective = result.Value;
        return new CatalogProductNameRecognitionPreviewResult(
            schema?.ProductTypeId, schema?.ProductTypeCode, schema?.ProductTypeName,
            definitions?.Select(definition => definition.Code).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code, StringComparer.OrdinalIgnoreCase).ToArray(),
            effective.RecognitionProfiles, effective.Recognition, query.ManufacturerId, manufacturerName,
            effective.HasCompleteScope, effective.ActiveRuleSet?.ActiveVersionId, effective.ActiveRuleSet?.SequenceNumber);
    }
}
