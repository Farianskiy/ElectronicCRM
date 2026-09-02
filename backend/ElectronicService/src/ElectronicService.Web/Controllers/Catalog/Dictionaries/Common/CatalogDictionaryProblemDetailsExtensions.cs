using ElectronicService.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Dictionaries.Common;

public static class CatalogDictionaryProblemDetailsExtensions
{
    private const string CurrentUserRequiredCode = "catalog.current_user.required";

    private const string DictionaryTermTechnicalUserRequiredCode = "catalog.dictionary_term.technical_user_required";

    private const string DictionarySuggestionTechnicalUserRequiredCode = "catalog.dictionary_suggestion.technical_user_required";

    private const string DictionarySuggestionReviewForbiddenCode = "catalog.dictionary_suggestion.user_cannot_review";

    private const string ProductTypeNotFoundCode = "catalog.product_type.not_found";

    private const string CharacteristicDefinitionNotFoundCode = "catalog.characteristic_definition.not_found";

    private const string DictionaryTermNotFoundCode = "catalog.dictionary_term.not_found";

    private const string DictionarySuggestionNotFoundCode = "catalog.dictionary_suggestion.not_found";

    private const string RecognitionCandidateNotFoundCode = "catalog.recognition_candidate.for_suggestion_not_found";

    private const string DictionaryTermAlreadyExistsCode = "catalog.dictionary_term.already_exists";

    private const string DictionaryTermInvalidStatusTransitionCode = "catalog.dictionary_term.invalid_status_transition";

    public static ObjectResult ToCatalogDictionaryProblem(this ControllerBase controller, DomainError error)
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

            DictionaryTermTechnicalUserRequiredCode or
            DictionarySuggestionTechnicalUserRequiredCode or
            DictionarySuggestionReviewForbiddenCode => StatusCodes.Status403Forbidden,

            ProductTypeNotFoundCode or
            CharacteristicDefinitionNotFoundCode or
            DictionaryTermNotFoundCode or
            DictionarySuggestionNotFoundCode => StatusCodes.Status404NotFound,

            DictionaryTermAlreadyExistsCode or
            DictionaryTermInvalidStatusTransitionCode or
            RecognitionCandidateNotFoundCode => StatusCodes.Status409Conflict,

            _ => StatusCodes.Status400BadRequest
        };
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Некорректные данные словарного термина",
            StatusCodes.Status401Unauthorized => "Требуется авторизация",
            StatusCodes.Status403Forbidden => "Недостаточно прав",
            StatusCodes.Status404NotFound => "Объект не найден",
            StatusCodes.Status409Conflict => "Операция конфликтует с текущим состоянием",
            _ => "Ошибка управления словарём"
        };
    }
}