using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users;
using ElectronicService.Domain.Users.Errors;

namespace ElectronicService.Core.Users.CreateRegularUser;

public sealed class CreateRegularUserCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRegularUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        CreateRegularUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var userResult = User.CreateRegular(
            command.DisplayName,
            command.Email);

        if (userResult.IsFailure)
        {
            return userResult.Error;
        }

        var user = userResult.Value;

        if (user.Email is not null)
        {
            var emailAlreadyExists = await _userRepository.ExistsByEmailAsync(
                user.Email,
                cancellationToken);

            if (emailAlreadyExists)
            {
                return UserErrors.EmailAlreadyTaken();
            }
        }

        _userRepository.Add(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}