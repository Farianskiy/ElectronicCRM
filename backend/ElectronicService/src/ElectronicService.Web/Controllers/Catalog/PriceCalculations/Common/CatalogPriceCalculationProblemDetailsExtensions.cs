using ElectronicService.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;

public static class CatalogPriceCalculationProblemDetailsExtensions
{
    public static ObjectResult
        ToCatalogPriceCalculationProblem(
            this ControllerBase controller,
            DomainError error,
            string title)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var statusCode =
            GetStatusCode(error.Code);

        return controller.StatusCode(
            statusCode,
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = error.Message,
                Type = error.Code
            });
    }

    public static ObjectResult ToCurrentUserProblem(
        this ControllerBase controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        return controller.Problem(
            statusCode:
                StatusCodes.Status401Unauthorized,
            title:
                "Пользователь не определён.",
            detail:
                "В запросе отсутствует корректный идентификатор пользователя.");
    }

    private static int GetStatusCode(
        string errorCode)
    {
        return errorCode switch
        {
            "catalog.price_calculation.current_user_not_found"
                => StatusCodes.Status401Unauthorized,

            "catalog.price_calculation.create_forbidden"
                or "catalog.price_calculation.modify_forbidden"
                or "catalog.price_calculation.view_forbidden"
                => StatusCodes.Status403Forbidden,

            "catalog.price_calculation.not_found"
                or "catalog.price_calculation.line.not_found"
                or "catalog.price_calculation.discount.not_found"
                => StatusCodes.Status404NotFound,

            "catalog.price_calculation.cannot_modify"
                or "catalog.price_calculation.invalid_status_transition"
                or "catalog.price_calculation.lines_required"
                or "catalog.price_calculation.line.duplicate_price_list_row"
                or "catalog.price_calculation.active_price_not_found"
                or "catalog.price_calculation.active_price_ambiguous"
                or "catalog.price_calculation.discount.manufacturer_has_no_lines"
                => StatusCodes.Status409Conflict,

            _ => StatusCodes.Status400BadRequest
        };
    }
}