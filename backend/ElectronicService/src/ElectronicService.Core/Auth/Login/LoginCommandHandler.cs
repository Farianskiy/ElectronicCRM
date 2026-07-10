using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Errors;
using ElectronicService.Domain.Users.ValueObjects;

namespace ElectronicService.Core.Auth.Login;

public sealed class LoginCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenProvider _jwtTokenProvider;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenProvider jwtTokenProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenProvider = jwtTokenProvider;
    }

    public async Task<Result<LoginResult, DomainError>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var emailResult = Email.Create(command.Email);

        if (emailResult.IsFailure)
        {
            return emailResult.Error;
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return GeneralErrors.ValueIsInvalid(nameof(command.Password));
        }

        var user = await _userRepository
            .GetByEmailAsync(emailResult.Value, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return GeneralErrors.ValueIsInvalid("emailOrPassword");
        }

        if (!user.IsActive)
        {
            return UserErrors.BlockedUserCannotBeChanged();
        }

        if (!user.HasPassword || user.PasswordHash is null)
        {
            return GeneralErrors.ValueIsInvalid("password");
        }

        var passwordIsValid = _passwordHasher.Verify(
            command.Password,
            user.PasswordHash);

        if (!passwordIsValid)
        {
            return GeneralErrors.ValueIsInvalid("emailOrPassword");
        }

        var accessToken = _jwtTokenProvider.CreateToken(user);

        return new LoginResult(
            accessToken,
            user.Id,
            user.Type.ToString(),
            user.DisplayName.Value);
    }
}