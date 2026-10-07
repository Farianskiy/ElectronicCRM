using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.ApplyCatalogPriceCalculationImport;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.ApplyCatalogPriceCalculationImport;

[ApiController]
[PermissionAuthorize(UserPermissionCode.PriceCalculationsManage)]
[Route("api/catalog/price-calculations")]
public sealed class ApplyCatalogPriceCalculationImportController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось применить изменения из файла.";

    [HttpPost("{calculationId:guid}/import-apply")]
    [ProducesResponseType(
        typeof(ApplyCatalogPriceCalculationImportResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<
        ApplyCatalogPriceCalculationImportResponse>> Apply(
            Guid calculationId,
            [FromBody]
            ApplyCatalogPriceCalculationImportRequest request,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            ApplyCatalogPriceCalculationImportCommandHandler handler,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(
            currentUserProvider);
        ArgumentNullException.ThrowIfNull(handler);

        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        if ((request.Rows is null || request.Rows.Count == 0)
            && (request.ComponentRows is null || request.ComponentRows.Count == 0)
            && (request.CharacteristicRows is null || request.CharacteristicRows.Count == 0))
        {
            return Problem(
                detail:
                    "В файле нет изменений, которые можно применить.",
                statusCode:
                    StatusCodes.Status400BadRequest,
                title: ProblemTitle);
        }

        var commandRows = new List<ApplyCatalogPriceCalculationImportRow>(
            request.Rows?.Count ?? 0);

        foreach (var row in request.Rows ?? [])
        {
            if (!Enum.TryParse<CatalogPriceCalculationImportAction>(
                    row.Action,
                    ignoreCase: true,
                    out var action)
                || !Enum.IsDefined(action))
            {
                return Problem(
                    detail: $"Неизвестное действие импорта: '{row.Action}'.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: ProblemTitle);
            }

            commandRows.Add(
                new ApplyCatalogPriceCalculationImportRow(
                    action,
                    row.ProductId,
                    row.ExistingLineId,
                    row.Quantity));
        }

        var componentCommandRows =
            new List<ApplyCatalogPriceCalculationComponentImportRow>(
                request.ComponentRows?.Count ?? 0);

        foreach (var row in request.ComponentRows ?? [])
        {
            if (!Enum.TryParse<CatalogPriceCalculationImportAction>(
                    row.Action,
                    ignoreCase: true,
                    out var action)
                || !Enum.IsDefined(action))
            {
                return Problem(
                    detail: $"Неизвестное действие импорта комплектующего: '{row.Action}'.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: ProblemTitle);
            }

            componentCommandRows.Add(
                new ApplyCatalogPriceCalculationComponentImportRow(
                    action,
                    row.MainLineId,
                    row.ExistingComponentLineId,
                    row.NeedDefinitionId,
                    row.ComponentProductId,
                    row.QuantityPerUnit));
        }

        var characteristicCommandRows =
            new List<ApplyCatalogPriceCalculationCharacteristicImportRow>(
                request.CharacteristicRows?.Count ?? 0);

        foreach (var row in request.CharacteristicRows ?? [])
        {
            if (!Enum.TryParse<CatalogPriceCalculationCharacteristicImportAction>(
                    row.Action,
                    ignoreCase: true,
                    out var action)
                || !Enum.IsDefined(action))
            {
                return Problem(
                    detail: $"Неизвестное действие импорта характеристики: '{row.Action}'.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: ProblemTitle);
            }

            characteristicCommandRows.Add(
                new ApplyCatalogPriceCalculationCharacteristicImportRow(
                    action,
                    row.ProductId,
                    row.CharacteristicCode,
                    row.Value));
        }

        var command =
            new ApplyCatalogPriceCalculationImportCommand(
                calculationId,
                commandRows,
                componentCommandRows,
                characteristicCommandRows,
                currentUserProvider.UserId.Value);

        var result =
            await handler
                .Handle(command, cancellationToken)
                .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogPriceCalculationProblem(
                result.Error,
                ProblemTitle);
        }

        return Ok(
            new ApplyCatalogPriceCalculationImportResponse(
                result.Value.CalculationId,
                result.Value.AddedLinesCount,
                result.Value.UpdatedLinesCount,
                result.Value.RemovedLinesCount,
                result.Value.AddedComponentsCount,
                result.Value.UpdatedComponentsCount,
                result.Value.RemovedComponentsCount,
                result.Value.UpdatedCharacteristicsCount,
                result.Value.RemovedCharacteristicsCount,
                result.Value.CalculationTotalAmount));
    }
}
