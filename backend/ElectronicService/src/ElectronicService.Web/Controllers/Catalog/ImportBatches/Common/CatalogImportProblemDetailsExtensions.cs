using ElectronicService.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;

public static class CatalogImportProblemDetailsExtensions
{
    public static ObjectResult ToCatalogImportProblem(
        this ControllerBase controller,
        DomainError error,
        string title)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var statusCode = GetStatusCode(error.Code);

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

    public static ObjectResult ToCurrentUserProblem(this ControllerBase controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        return controller.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Пользователь не определён.",
            detail: "В JWT отсутствует корректный идентификатор пользователя.");
    }

    private static int GetStatusCode(string errorCode)
    {
        return errorCode switch
        {
            "catalog.import.current_user.not_found"
                => StatusCodes.Status401Unauthorized,

            "catalog.import.batch.access_denied"
                or "catalog.import.user.cannot_create"
                or "catalog.import.user.cannot_submit"
                or "catalog.import.user.cannot_edit"
                or "catalog.import.user.cannot_review"
                or "catalog.import.user.cannot_apply"
                or "catalog.import.user.cannot_view_own"
                or "catalog.import.user.cannot_delete"
                or "catalog.import.batch.cannot_be_applied_by_current_user"
                => StatusCodes.Status403Forbidden,

            "catalog.import.batch.not_found"
                or "catalog.import.product_type.not_found"
                or "catalog.import.row.not_found"
                or "catalog.import.manufacturer.not_found"
                or "catalog.import.apply.characteristic_definition_not_found"
                => StatusCodes.Status404NotFound,

            "catalog.import.batch.invalid_status_transition"
                or "catalog.import.batch.product_type_required"
                or "catalog.import.batch.rows_cannot_be_edited"
                or "catalog.import.batch.cannot_be_analyzed"
                or "catalog.import.batch.concurrency_conflict"
                or "catalog.import.batch.review_assigned_to_another_user"
                or "catalog.import.batch.cannot_be_deleted"
                or "catalog.import.mapping.cannot_be_edited"
                or "catalog.import.column.duplicate_mapping"
                or "catalog.import.apply.duplicate_article"
                or "catalog.import.apply.article_already_exists"
                or "catalog.import.apply.concurrency_conflict"
                or "catalog.import.applied_products.unavailable"
                or "catalog.import.error_report.unavailable"
                => StatusCodes.Status409Conflict,

            "catalog.import.file.too_large"
                => StatusCodes.Status413PayloadTooLarge,

            "catalog.import.file.unsupported_extension"
                => StatusCodes.Status415UnsupportedMediaType,

            "catalog.import.row.invalid_json"
                or "catalog.import.file.cannot_be_read"
                or "catalog.import.file.integrity_check_failed"
                or "catalog.import.apply.database_failure"
                => StatusCodes.Status500InternalServerError,

            _ => StatusCodes.Status400BadRequest
        };
    }
}