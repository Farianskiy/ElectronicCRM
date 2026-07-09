using ElectronicService.Contracts.Catalog.Dictionaries;
using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Dictionaries.GetTerms;

[ApiController]
[Route("api/catalog/dictionary/terms")]
public sealed class CatalogDictionaryTermsController : ControllerBase
{
    private readonly GetCatalogDictionaryTermsQueryHandler _handler;

    public CatalogDictionaryTermsController(
        GetCatalogDictionaryTermsQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CatalogDictionaryTermResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CatalogDictionaryTermResponse>>> GetTerms(
        CancellationToken cancellationToken = default)
    {
        var result = await _handler
            .Handle(new GetCatalogDictionaryTermsQuery(), cancellationToken)
            .ConfigureAwait(false);

        return Ok(result.Select(term => new CatalogDictionaryTermResponse(
            term.Id,
            term.Phrase,
            term.NormalizedPhrase,
            term.Kind,
            term.TargetCode,
            term.TargetValue,
            term.Priority,
            term.Status,
            term.Source)).ToList());
    }
}