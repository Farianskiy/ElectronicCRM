using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users;
using ElectronicService.Domain.Users.Errors;

namespace ElectronicService.Core.Users.CreateAdministratorUser;

public sealed class CreateAdministratorUserCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public CreateAdministratorUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid, DomainError>> Handle(CreateAdministratorUserCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var userResult = User.CreateAdministrator(command.DisplayName, command.Email, _passwordHasher.Hash(command.Password));

        if (userResult.IsFailure)
        {
            return userResult.Error;
        }

        var user = userResult.Value;

        if (await _userRepository.ExistsByEmailAsync(user.Email!, cancellationToken).ConfigureAwait(false))
        {
            return UserErrors.EmailAlreadyTaken();
        }

        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return user.Id;
    }
}