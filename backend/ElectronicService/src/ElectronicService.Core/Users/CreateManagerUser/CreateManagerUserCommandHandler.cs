using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users;
using ElectronicService.Domain.Users.Errors;

namespace ElectronicService.Core.Users.CreateManagerUser;

public sealed class CreateManagerUserCommandHandler
{
    private readonly IUserRepository
        _userRepository;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly IPasswordHasher
        _passwordHasher;

    public CreateManagerUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        CreateManagerUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var passwordHash =
            _passwordHasher.Hash(
                command.Password);

        var userResult = User.CreateManager(
            command.DisplayName,
            command.Email,
            passwordHash);

        if (userResult.IsFailure)
        {
            return userResult.Error;
        }

        var user = userResult.Value;

        var emailAlreadyExists =
            await _userRepository
                .ExistsByEmailAsync(
                    user.Email!,
                    cancellationToken)
                .ConfigureAwait(false);

        if (emailAlreadyExists)
        {
            return UserErrors.EmailAlreadyTaken();
        }

        _userRepository.Add(user);

        await _unitOfWork
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return user.Id;
    }
}