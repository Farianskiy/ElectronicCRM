using ElectronicService.Contracts.Catalog.Manufacturers.Recognition;
using ElectronicService.Core.Catalog.Manufacturers.PreviewRecognition;
using ElectronicService.Core.Catalog.Manufacturers.Resolution;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Manufacturers.PreviewRecognition;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/manufacturers/recognition/preview")]
public sealed class PreviewManufacturerNameRecognitionController : ControllerBase
{
    private readonly PreviewManufacturerNameRecognitionQueryHandler _handler;

    public PreviewManufacturerNameRecognitionController(
        PreviewManufacturerNameRecognitionQueryHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ManufacturerNameRecognitionPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ManufacturerNameRecognitionPreviewResponse>> Preview(
        [FromBody] PreviewManufacturerNameRecognitionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            return BadRequest("Наименование товара обязательно.");
        }

        var result = await _handler
            .Handle(
                new PreviewManufacturerNameRecognitionQuery(request.ProductName),
                cancellationToken)
            .ConfigureAwait(false);

        var selectedCandidate = result.SelectedCandidate is null
            ? null
            : MapCandidate(result.SelectedCandidate);

        var response = new ManufacturerNameRecognitionPreviewResponse(
            result.ProductName,
            result.Status.ToString(),
            result.IsResolved,
            result.IsConflict,
            selectedCandidate,
            result.Candidates
                .Select(MapCandidate)
                .ToArray());

        return Ok(response);
    }

    private static ManufacturerNameRecognitionCandidateResponse MapCandidate(
        ManufacturerNameRecognitionCandidate candidate)
    {
        return new ManufacturerNameRecognitionCandidateResponse(
            candidate.ManufacturerId,
            candidate.ManufacturerName,
            candidate.RawValue,
            candidate.NormalizedValue,
            candidate.Confidence,
            candidate.Source.ToString(),
            candidate.ManufacturerAliasId,
            candidate.StartIndex,
            candidate.Length,
            candidate.EndIndex);
    }
}