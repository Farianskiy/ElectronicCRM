using ElectronicService.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceLists.Common;

public static class CatalogPriceListProblemDetailsExtensions
{
    public static ObjectResult ToCatalogPriceListProblem(
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
            "catalog.price_list.current_user_not_found"
                => StatusCodes.Status401Unauthorized,

            "catalog.price_list.upload_forbidden"
                or "catalog.price_list.process_forbidden"
                or "catalog.price_list.view_forbidden"
                or "catalog.price_list.edit_forbidden"
                or "catalog.price_list.activate_forbidden"
                => StatusCodes.Status403Forbidden,

            "catalog.manufacturer.not_found"
                or "catalog.price_list.not_found"
                or "catalog.price_list.row.not_found"
                or "catalog.price_list.issue_group.not_found"
                => StatusCodes.Status404NotFound,

            "catalog.price_list.file.too_large"
                => StatusCodes.Status413PayloadTooLarge,

            "catalog.price_list.file.unsupported_extension"
                => StatusCodes.Status415UnsupportedMediaType,

            "catalog.price_list.file.cannot_be_read"
                or "catalog.price_list.processing_failed"
                or "catalog.price_list.row.update_failed"
                or "catalog.price_list.activation_failed"
                => StatusCodes.Status500InternalServerError,

            "catalog.price_list.invalid_status_transition"
                or "catalog.price_list.rows.cannot_be_edited"
                or "catalog.price_list.row.product_manufacturer_mismatch"
                => StatusCodes.Status409Conflict,

            _ => StatusCodes.Status400BadRequest
        };
    }
}