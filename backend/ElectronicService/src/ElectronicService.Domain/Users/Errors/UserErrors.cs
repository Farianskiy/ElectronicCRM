using ElectronicService.Domain.Common;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ElectronicService.Domain.Users.Errors;

public static class UserErrors
{
    public static DomainError AlreadyTechnical()
    {
        return new DomainError(
            "user.already_technical",
            "Пользователь уже является техническим пользователем.");
    }

    public static DomainError AlreadyRegular()
    {
        return new DomainError(
            "user.already_regular",
            "Пользователь уже является обычным пользователем.");
    }

    public static DomainError AlreadyBlocked()
    {
        return new DomainError(
            "user.already_blocked",
            "Пользователь уже заблокирован.");
    }

    public static DomainError AlreadyActive()
    {
        return new DomainError(
            "user.already_active",
            "Пользователь уже активен.");
    }

    public static DomainError TechnicalUserEmailIsRequired()
    {
        return new DomainError(
            "user.technical_user_email_is_required",
            "Для технического пользователя email обязателен.");
    }

    public static DomainError BlockedUserCannotBeChanged()
    {
        return new DomainError(
            "user.blocked_user_cannot_be_changed",
            "Заблокированного пользователя нельзя изменять.");
    }

    public static DomainError NotFound(Guid userId)
    {
        return new DomainError(
            "user.not_found",
            $"Пользователь с идентификатором '{userId}' не найден.");
    }

    public static DomainError EmailAlreadyTaken()
    {
        return new DomainError(
            "user.email_already_taken",
            "Пользователь с таким email уже существует.");
    }
}