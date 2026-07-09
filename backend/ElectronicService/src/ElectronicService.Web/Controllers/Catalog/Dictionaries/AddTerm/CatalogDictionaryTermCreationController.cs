using ElectronicService.Contracts.Catalog.Dictionaries;
using ElectronicService.Core.Catalog.Dictionaries.AddTerm;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Dictionaries.AddTerm;

[ApiController]
[Route("api/catalog/dictionary/terms")]
public sealed class CatalogDictionaryTermCreationController : ControllerBase
{
    private readonly AddCatalogDictionaryTermCommandHandler _handler;

    public CatalogDictionaryTermCreationController(
        AddCatalogDictionaryTermCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CatalogDictionaryTermResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CatalogDictionaryTermResponse>> AddTerm(
        [FromBody] AddCatalogDictionaryTermRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new AddCatalogDictionaryTermCommand(
            request.Phrase,
            request.Kind,
            request.TargetCode,
            request.TargetValue,
            request.Priority);

        var result = await _handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return BadRequest(result.Error.Message);
        }

        var response = new CatalogDictionaryTermResponse(
            result.Value.Id,
            result.Value.Phrase,
            NormalizeText(result.Value.Phrase),
            result.Value.Kind,
            result.Value.TargetCode,
            result.Value.TargetValue,
            result.Value.Priority,
            result.Value.Status,
            result.Value.Source);

        return Created(
            new Uri($"/api/catalog/dictionary/terms/{result.Value.Id}", UriKind.Relative),
            response);
    }

    private static string NormalizeText(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);
    }
}