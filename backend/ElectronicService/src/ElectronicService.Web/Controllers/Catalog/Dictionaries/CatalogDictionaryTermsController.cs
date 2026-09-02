using ElectronicService.Contracts.Catalog.Dictionaries;
using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Dictionaries.GetTerms;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/dictionary/terms")]
public sealed class CatalogDictionaryTermsController : ControllerBase
{
    private readonly GetCatalogDictionaryTermsQueryHandler _handler;

    public CatalogDictionaryTermsController(GetCatalogDictionaryTermsQueryHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CatalogDictionaryTermResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<CatalogDictionaryTermResponse>>> GetTerms(CancellationToken cancellationToken = default)
    {
        var result = await _handler.Handle(new GetCatalogDictionaryTermsQuery(), cancellationToken).ConfigureAwait(false);

        var response = result
            .Select(term => new CatalogDictionaryTermResponse(
                term.Id,
                term.ProductTypeId,
                term.Phrase,
                term.NormalizedPhrase,
                term.Kind,
                term.TargetCode,
                term.TargetValue,
                term.Priority,
                term.Status,
                term.Source,
                term.CreatedAtUtc,
                term.ApprovedAtUtc,
                term.DisabledAtUtc,
                term.DisabledByUserId,
                term.DisabledByUserDisplayName,
                term.DisableReason,
                term.ReactivatedAtUtc,
                term.ReactivatedByUserId,
                term.ReactivatedByUserDisplayName))
            .ToList();

        return Ok(response);
    }
}