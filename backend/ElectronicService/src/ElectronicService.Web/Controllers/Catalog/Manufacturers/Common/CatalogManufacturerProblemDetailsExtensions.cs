using ElectronicService.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Manufacturers.Common;

public static class CatalogManufacturerProblemDetailsExtensions
{
    private const string CurrentUserRequiredCode = "catalog.current_user.required";
    private const string TechnicalUserRequiredCode = "catalog.manufacturer.technical_user_required";
    private const string ManufacturerNotFoundCode = "catalog.manufacturer.not_found";
    private const string ManufacturerAlreadyExistsCode = "catalog.manufacturer.already_exists";
    private const string ManufacturerNameConflictsWithAliasCode = "catalog.manufacturer.name_conflicts_with_alias";
    private const string ManufacturerNameConflictsWithNoisePhraseCode = "catalog.manufacturer.name_conflicts_with_noise_phrase";
    private const string AliasAlreadyExistsCode = "catalog.manufacturer_alias.already_exists";
    private const string AliasConflictsWithManufacturerNameCode = "catalog.manufacturer_alias.conflicts_with_manufacturer_name";
    private const string AliasConflictsWithNoisePhraseCode = "catalog.manufacturer_alias.conflicts_with_noise_phrase";
    private const string NoisePhraseConflictsWithManufacturerNameCode = "catalog.manufacturer_noise_phrase.conflicts_with_manufacturer_name";
    private const string NoisePhraseConflictsWithManufacturerAliasCode = "catalog.manufacturer_noise_phrase.conflicts_with_manufacturer_alias";

    public static ObjectResult ToCatalogManufacturerProblem(this ControllerBase controller, DomainError error)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(error);

        var statusCode = GetStatusCode(error.Code);

        var result = controller.Problem(
            detail: error.Message,
            instance: controller.HttpContext.Request.Path,
            statusCode: statusCode,
            title: GetTitle(statusCode));

        if (result.Value is ProblemDetails problemDetails)
        {
            problemDetails.Extensions["code"] = error.Code;
        }

        return result;
    }

    private static int GetStatusCode(string errorCode)
    {
        return errorCode switch
        {
            CurrentUserRequiredCode => StatusCodes.Status401Unauthorized,
            TechnicalUserRequiredCode => StatusCodes.Status403Forbidden,
            ManufacturerNotFoundCode => StatusCodes.Status404NotFound,
            ManufacturerAlreadyExistsCode => StatusCodes.Status409Conflict,
            ManufacturerNameConflictsWithAliasCode => StatusCodes.Status409Conflict,
            ManufacturerNameConflictsWithNoisePhraseCode => StatusCodes.Status409Conflict,
            AliasAlreadyExistsCode => StatusCodes.Status409Conflict,
            AliasConflictsWithManufacturerNameCode => StatusCodes.Status409Conflict,
            AliasConflictsWithNoisePhraseCode => StatusCodes.Status409Conflict,
            NoisePhraseConflictsWithManufacturerNameCode => StatusCodes.Status409Conflict,
            NoisePhraseConflictsWithManufacturerAliasCode => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Некорректные данные производителя",
            StatusCodes.Status401Unauthorized => "Требуется авторизация",
            StatusCodes.Status403Forbidden => "Недостаточно прав",
            StatusCodes.Status404NotFound => "Производитель не найден",
            StatusCodes.Status409Conflict => "Конфликт данных производителя",
            _ => "Ошибка управления производителями"
        };
    }
}