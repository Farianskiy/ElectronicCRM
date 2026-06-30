using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users;
using ElectronicService.Domain.Users.Errors;

namespace ElectronicService.Core.Users.CreateTechnicalUser;

public sealed class CreateTechnicalUserCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTechnicalUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        CreateTechnicalUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var userResult = User.CreateTechnical(
            command.DisplayName,
            command.Email);

        if (userResult.IsFailure)
        {
            return userResult.Error;
        }

        var user = userResult.Value;

        var emailAlreadyExists = await _userRepository.ExistsByEmailAsync(
            user.Email!,
            cancellationToken);

        if (emailAlreadyExists)
        {
            return UserErrors.EmailAlreadyTaken();
        }

        _userRepository.Add(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}