using ElectronicService.Contracts.Catalog.Dictionaries;
using ElectronicService.Core.Catalog.Dictionaries.AddTerm;
using ElectronicService.Web.Controllers.Catalog.Dictionaries.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Dictionaries.AddTerm;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/dictionary/terms")]
public sealed class CatalogDictionaryTermCreationController : ControllerBase
{
    private readonly AddCatalogDictionaryTermCommandHandler _handler;

    public CatalogDictionaryTermCreationController(
        AddCatalogDictionaryTermCommandHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(AddCatalogDictionaryTermResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AddCatalogDictionaryTermResponse>> AddTerm(
        [FromBody] AddCatalogDictionaryTermRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new AddCatalogDictionaryTermCommand(
            request.ProductTypeCode,
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
            return this.ToCatalogDictionaryProblem(
                result.Error);
        }

        var response = new AddCatalogDictionaryTermResponse(
            result.Value.Id,
            result.Value.ProductTypeId,
            result.Value.ProductTypeCode,
            result.Value.Phrase,
            result.Value.NormalizedPhrase,
            result.Value.Kind,
            result.Value.TargetCode,
            result.Value.TargetValue,
            result.Value.Priority,
            result.Value.Status,
            result.Value.Source);

        return Created(
            new Uri(
                $"/api/catalog/dictionary/terms/{result.Value.Id}",
                UriKind.Relative),
            response);
    }
}