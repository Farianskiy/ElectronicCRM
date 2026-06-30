using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Errors;

namespace ElectronicService.Core.Users.MakeUserRegular;

public sealed class MakeUserRegularCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MakeUserRegularCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UnitResult<DomainError>> Handle(
        MakeUserRegularCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(
            command.UserId,
            cancellationToken);

        if (user is null)
        {
            return UnitResult.Failure(UserErrors.NotFound(command.UserId));
        }

        var makeRegularResult = user.MakeRegular();

        if (makeRegularResult.IsFailure)
        {
            return makeRegularResult;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<DomainError>();
    }
}