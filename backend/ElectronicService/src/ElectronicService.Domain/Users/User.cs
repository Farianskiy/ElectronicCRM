using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Domain.Users.Errors;
using ElectronicService.Domain.Users.ValueObjects;

namespace ElectronicService.Domain.Users;

public sealed class User : AggregateRoot
{
    private User(
        Guid id,
        UserDisplayName displayName,
        Email? email,
        UserType type,
        string? passwordHash)
        : base(id)
    {
        DisplayName = displayName;
        Email = email;
        Type = type;
        PasswordHash = passwordHash;
        Status = UserStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private User()
    {
    }

    public UserDisplayName DisplayName { get; private set; } = null!;

    public Email? Email { get; private set; }

    public UserType Type { get; private set; }

    public UserStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsRegular => Type == UserType.Regular;

    public bool IsTechnical => Type == UserType.Technical;

    public bool IsActive => Status == UserStatus.Active;

    public bool IsBlocked => Status == UserStatus.Blocked;

    public string? PasswordHash { get; private set; }

    public bool HasPassword => !string.IsNullOrWhiteSpace(PasswordHash);

    public bool CanUseAssistant()
    {
        return IsActive;
    }

    public bool CanViewProducts()
    {
        return IsActive;
    }

    public bool CanFindProductAlternatives()
    {
        return IsActive;
    }

    public bool CanUpdateProductPrice()
    {
        return IsActive && IsTechnical;
    }

    public bool CanUpdateStockBalance()
    {
        return IsActive && IsTechnical;
    }

    public bool CanApproveProductCorrections()
    {
        return IsActive && IsTechnical;
    }

    public bool CanManageProductSynonyms()
    {
        return IsActive && IsTechnical;
    }

    public static Result<User, DomainError> CreateRegular(
        string displayName,
        string? email,
        string? passwordHash = null)
    {
        var displayNameResult = UserDisplayName.Create(displayName);

        if (displayNameResult.IsFailure)
        {
            return displayNameResult.Error;
        }

        Email? userEmail = null;

        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailResult = Email.Create(email);

            if (emailResult.IsFailure)
            {
                return emailResult.Error;
            }

            userEmail = emailResult.Value;
        }

        return new User(
            Guid.CreateVersion7(),
            displayNameResult.Value,
            userEmail,
            UserType.Regular,
            NormalizePasswordHash(passwordHash));
    }

    public static Result<User, DomainError> CreateTechnical(
        string displayName,
        string email,
        string? passwordHash = null)
    {
        var displayNameResult = UserDisplayName.Create(displayName);

        if (displayNameResult.IsFailure)
        {
            return displayNameResult.Error;
        }

        var emailResult = Email.Create(email);

        if (emailResult.IsFailure)
        {
            return emailResult.Error;
        }

        return new User(
            Guid.CreateVersion7(),
            displayNameResult.Value,
            emailResult.Value,
            UserType.Technical,
            NormalizePasswordHash(passwordHash));
    }

    public UnitResult<DomainError> ChangeDisplayName(string displayName)
    {
        if (IsBlocked)
        {
            return UnitResult.Failure(UserErrors.BlockedUserCannotBeChanged());
        }

        var displayNameResult = UserDisplayName.Create(displayName);

        if (displayNameResult.IsFailure)
        {
            return UnitResult.Failure(displayNameResult.Error);
        }

        DisplayName = displayNameResult.Value;
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> ChangeEmail(string email)
    {
        if (IsBlocked)
        {
            return UnitResult.Failure(UserErrors.BlockedUserCannotBeChanged());
        }

        var emailResult = Email.Create(email);

        if (emailResult.IsFailure)
        {
            return UnitResult.Failure(emailResult.Error);
        }

        Email = emailResult.Value;
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> MakeTechnical(string email)
    {
        if (IsBlocked)
        {
            return UnitResult.Failure(UserErrors.BlockedUserCannotBeChanged());
        }

        if (IsTechnical)
        {
            return UnitResult.Failure(UserErrors.AlreadyTechnical());
        }

        var emailResult = Email.Create(email);

        if (emailResult.IsFailure)
        {
            return UnitResult.Failure(emailResult.Error);
        }

        Email = emailResult.Value;
        Type = UserType.Technical;
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> MakeRegular()
    {
        if (IsBlocked)
        {
            return UnitResult.Failure(UserErrors.BlockedUserCannotBeChanged());
        }

        if (IsRegular)
        {
            return UnitResult.Failure(UserErrors.AlreadyRegular());
        }

        Type = UserType.Regular;
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Block()
    {
        if (IsBlocked)
        {
            return UnitResult.Failure(UserErrors.AlreadyBlocked());
        }

        Status = UserStatus.Blocked;
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Activate()
    {
        if (IsActive)
        {
            return UnitResult.Failure(UserErrors.AlreadyActive());
        }

        Status = UserStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> ChangePasswordHash(string passwordHash)
    {
        if (IsBlocked)
        {
            return UnitResult.Failure(UserErrors.BlockedUserCannotBeChanged());
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(passwordHash)));
        }

        PasswordHash = passwordHash.Trim();
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    private static string? NormalizePasswordHash(string? passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return null;
        }

        return passwordHash.Trim();
    }
}