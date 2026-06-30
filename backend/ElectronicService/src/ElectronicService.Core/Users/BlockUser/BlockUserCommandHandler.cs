using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Errors;

namespace ElectronicService.Core.Users.BlockUser;

public sealed class BlockUserCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BlockUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UnitResult<DomainError>> Handle(
        BlockUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(
            command.UserId,
            cancellationToken);

        if (user is null)
        {
            return UnitResult.Failure(UserErrors.NotFound(command.UserId));
        }

        var blockResult = user.Block();

        if (blockResult.IsFailure)
        {
            return blockResult;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<DomainError>();
    }
}