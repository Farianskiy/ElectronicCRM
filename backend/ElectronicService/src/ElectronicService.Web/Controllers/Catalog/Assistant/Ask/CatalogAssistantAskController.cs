using ElectronicService.Contracts.Catalog.Assistant;
using ElectronicService.Contracts.Catalog.Products;
using ElectronicService.Contracts.Catalog.Products.Replacements;
using ElectronicService.Core.Catalog.Assistant.AskCatalogAssistant;
using ElectronicService.Core.Catalog.Manufacturers.Resolution;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Assistant.Ask;

[ApiController]
[Route("api/catalog/assistant/ask")]
public sealed class CatalogAssistantAskController : ControllerBase
{
    private readonly AskCatalogAssistantCommandHandler _handler;

    public CatalogAssistantAskController(
        AskCatalogAssistantCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CatalogAssistantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CatalogAssistantResponse>> Ask(
        [FromBody] AskCatalogAssistantRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new AskCatalogAssistantCommand(
            request.Message,
            request.OnlyInStock,
            request.MinimumScore,
            request.Page,
            request.PageSize,
            request.SelectedManufacturer);

        var result = await _handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return BadRequest(result.Error.Message);
        }

        var clarification = result.Value.ParsedRequest.Clarification;

        var clarificationResponse = clarification is null
            ? null
            : new CatalogAssistantClarificationResponse(
                clarification.UnknownPhrase,
                clarification.SuggestedKind,
                clarification.SuggestedTargetCode,
                clarification.SuggestedTargetValue,
                clarification.Confidence,
                clarification.Question,
                clarification.CanCreateSuggestion);

        var manufacturerRecognitionResponse = MapManufacturerRecognition(
            result.Value.ParsedRequest.ManufacturerRecognition);

        return Ok(new CatalogAssistantResponse(
            result.Value.Intent.ToString(),
            clarification is not null,
            result.Value.Answer,
            new CatalogAssistantParsedRequestResponse(
                result.Value.ParsedRequest.Intent.ToString(),
                result.Value.ParsedRequest.Search,
                result.Value.ParsedRequest.ProductTypeCode,
                result.Value.ParsedRequest.Manufacturer,
                result.Value.ParsedRequest.Characteristics
                    .Select(characteristic => new CatalogAssistantCharacteristicResponse(
                        characteristic.Code,
                        characteristic.Value))
                    .ToList(),
                clarificationResponse,
                manufacturerRecognitionResponse),
            result.Value.Products.Select(product => new ProductListItemResponse(
                product.Id,
                product.Article,
                product.Name,
                product.ProductTypeCode,
                product.ProductTypeName,
                product.ManufacturerName,
                product.PriceAmount,
                product.PriceCurrency,
                product.StockQuantity)).ToList(),
            result.Value.SourceProduct is null
                ? null
                : new ProductListItemResponse(
                    result.Value.SourceProduct.Id,
                    result.Value.SourceProduct.Article,
                    result.Value.SourceProduct.Name,
                    result.Value.SourceProduct.ProductTypeCode,
                    result.Value.SourceProduct.ProductTypeName,
                    result.Value.SourceProduct.ManufacturerName,
                    result.Value.SourceProduct.PriceAmount,
                    result.Value.SourceProduct.PriceCurrency,
                    result.Value.SourceProduct.StockQuantity),
            result.Value.Replacements.Select(replacement => new ProductReplacementItemResponse(
                replacement.Id,
                replacement.Article,
                replacement.Name,
                replacement.ProductTypeCode,
                replacement.ProductTypeName,
                replacement.ManufacturerName,
                replacement.PriceAmount,
                replacement.PriceCurrency,
                replacement.StockQuantity,
                replacement.ReplacementScore)).ToList()));
    }

    private static CatalogAssistantManufacturerRecognitionResponse MapManufacturerRecognition(
        ManufacturerNameRecognitionResult recognition)
    {
        var selectedCandidate = recognition.SelectedCandidate is null
            ? null
            : MapManufacturerCandidate(recognition.SelectedCandidate);

        return new CatalogAssistantManufacturerRecognitionResponse(
            recognition.ProductName,
            recognition.Status.ToString(),
            recognition.IsResolved,
            recognition.IsConflict,
            selectedCandidate,
            recognition.Candidates
                .Select(MapManufacturerCandidate)
                .ToArray());
    }

    private static CatalogAssistantManufacturerRecognitionCandidateResponse MapManufacturerCandidate(
        ManufacturerNameRecognitionCandidate candidate)
    {
        return new CatalogAssistantManufacturerRecognitionCandidateResponse(
            candidate.ManufacturerName,
            candidate.RawValue,
            candidate.NormalizedValue,
            candidate.Confidence,
            candidate.Source.ToString(),
            candidate.StartIndex,
            candidate.Length,
            candidate.EndIndex);
    }
}