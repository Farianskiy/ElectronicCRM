using ElectronicService.Contracts.Catalog.Assistant;
using ElectronicService.Contracts.Catalog.Assistant.PreviewBatch;
using ElectronicService.Contracts.Catalog.Products;
using ElectronicService.Core.Catalog.Assistant.PreviewCatalogAssistantBatch;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Assistant.PreviewBatch;

[ApiController]
[Route("api/catalog/assistant/preview-batch")]
public sealed class CatalogAssistantBatchPreviewController : ControllerBase
{
    private readonly PreviewCatalogAssistantBatchCommandHandler _handler;

    public CatalogAssistantBatchPreviewController(PreviewCatalogAssistantBatchCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PreviewCatalogAssistantBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PreviewCatalogAssistantBatchResponse>> Preview([FromBody] PreviewCatalogAssistantBatchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _handler.Handle(new PreviewCatalogAssistantBatchCommand(request.Message, request.OnlyInStock, request.MatchesPerLine), cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return BadRequest(result.Error.Message);
        }

        return Ok(new PreviewCatalogAssistantBatchResponse(result.Value.CommonText, result.Value.TotalLines, result.Value.MatchedLines, result.Value.RequiresAttentionLines, result.Value.Lines.Select(MapLine).ToArray()));
    }

    private static CatalogAssistantBatchLineResponse MapLine(CatalogAssistantBatchLineResult line)
    {
        return new CatalogAssistantBatchLineResponse(line.LineNumber, line.SourceText, line.SearchText, line.Quantity, line.Status, line.Message, line.ParsedRequest?.Manufacturer, line.ParsedRequest?.Characteristics.Select(characteristic => new CatalogAssistantCharacteristicResponse(characteristic.Code, characteristic.Value)).ToArray() ?? [], line.Products.Select(product => new ProductListItemResponse(product.Id, product.Article, product.Name, product.ProductTypeCode, product.ProductTypeName, product.ManufacturerName, product.PriceAmount, product.PriceCurrency, product.StockQuantity)).ToArray());
    }
}