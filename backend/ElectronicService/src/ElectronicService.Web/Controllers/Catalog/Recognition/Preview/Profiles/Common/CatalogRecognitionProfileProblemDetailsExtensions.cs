using ElectronicService.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.Profiles.Common;

public static class CatalogRecognitionProfileProblemDetailsExtensions
{
    private const string CurrentUserRequiredCode = "catalog.current_user.required";
    private const string UserCannotManageCode = "catalog.recognition.profile.user_cannot_manage";
    private const string ProductTypeNotFoundCode = "catalog.product_type.not_found";
    private const string ProfileNotFoundCode = "catalog.recognition.profile.not_found";
    private const string ProfileAlreadyExistsCode = "catalog.recognition.profile.already_exists";

    public static ObjectResult ToCatalogRecognitionProfileProblem(
        this ControllerBase controller,
        DomainError error)
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
            UserCannotManageCode => StatusCodes.Status403Forbidden,
            ProductTypeNotFoundCode => StatusCodes.Status404NotFound,
            ProfileNotFoundCode => StatusCodes.Status404NotFound,
            ProfileAlreadyExistsCode => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Некорректные данные профиля распознавания",
            StatusCodes.Status401Unauthorized => "Требуется авторизация",
            StatusCodes.Status403Forbidden => "Недостаточно прав",
            StatusCodes.Status404NotFound => "Объект не найден",
            StatusCodes.Status409Conflict => "Конфликт состояния",
            _ => "Ошибка управления профилем распознавания"
        };
    }
}