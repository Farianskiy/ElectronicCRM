using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Errors;
using ElectronicService.Domain.Users.ValueObjects;

namespace ElectronicService.Core.Users.MakeUserTechnical;

public sealed class MakeUserTechnicalCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MakeUserTechnicalCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UnitResult<DomainError>> Handle(
        MakeUserTechnicalCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(
            command.UserId,
            cancellationToken);

        if (user is null)
        {
            return UnitResult.Failure(UserErrors.NotFound(command.UserId));
        }

        var emailResult = Email.Create(command.Email);

        if (emailResult.IsFailure)
        {
            return UnitResult.Failure(emailResult.Error);
        }

        var emailAlreadyExists = await _userRepository.ExistsByEmailAsync(
            emailResult.Value,
            cancellationToken);

        if (emailAlreadyExists && !emailResult.Value.Equals(user.Email))
        {
            return UnitResult.Failure(UserErrors.EmailAlreadyTaken());
        }

        var makeTechnicalResult = user.MakeTechnical(command.Email);

        if (makeTechnicalResult.IsFailure)
        {
            return makeTechnicalResult;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<DomainError>();
    }
}