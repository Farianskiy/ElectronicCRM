using static System.Runtime.InteropServices.JavaScript.JSType;
namespace ElectronicService.Domain.Common;

public static class GeneralErrors
{
    public static DomainError ValueIsRequired(string fieldName)
    {
        return new DomainError(
            "general.value_is_required",
            $"Значение '{fieldName}' обязательно.");
    }

    public static DomainError ValueIsInvalid(string fieldName)
    {
        return new DomainError(
            "general.value_is_invalid",
            $"Значение '{fieldName}' некорректно.");
    }

    public static DomainError ValueIsTooLong(string fieldName, int maxLength)
    {
        return new DomainError(
            "general.value_is_too_long",
            $"Значение '{fieldName}' не должно быть длиннее {maxLength} символов.");
    }

    public static DomainError ValueIsTooShort(string fieldName, int minLength)
    {
        return new DomainError(
            "general.value_is_too_short",
            $"Значение '{fieldName}' не должно быть короче {minLength} символов.");
    }
}