using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Domain.Users.Errors;

namespace ElectronicService.Core.Users.Access;

public sealed class UpdateUserAccessCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionOverrideRepository _overrideRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserAccessCommandHandler(
        IUserRepository userRepository,
        IUserPermissionOverrideRepository overrideRepository,
        ICurrentUserProvider currentUserProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _overrideRepository = overrideRepository;
        _currentUserProvider = currentUserProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<UnitResult<DomainError>> Handle(
        UpdateUserAccessCommand command,
        CancellationToken cancellationToken = default)
    {
        var actorId = _currentUserProvider.UserId;

        var actor = actorId.HasValue
            ? await _userRepository
                .GetByIdAsync(actorId.Value, cancellationToken)
                .ConfigureAwait(false)
            : null;

        var target = await _userRepository
            .GetByIdAsync(command.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (actor is null || target is null || target.IsSystemDeveloper)
        {
            return UnitResult.Failure(
                UserErrors.NotFound(command.UserId));
        }

        if (command.UserType == UserType.Administrator &&
            target.Type != UserType.Administrator &&
            !actor.IsSystemDeveloper)
        {
            return UnitResult.Failure(
                UserErrors.AdministratorRoleRequiresSystemDeveloper());
        }

        var changeTypeResult = target.ChangeType(command.UserType);

        if (changeTypeResult.IsFailure)
        {
            return changeTypeResult;
        }

        var allowed = command.AllowedPermissions.ToHashSet();
        var defaults = UserPermissionCatalog.GetDefaults(command.UserType);

        var overrides = UserPermissionCatalog.Definitions
            .Where(definition =>
                allowed.Contains(definition.Code) !=
                defaults.Contains(definition.Code))
            .Select(definition => UserPermissionOverride.Create(
                target.Id,
                definition.Code,
                allowed.Contains(definition.Code)))
            .ToArray();

        await _overrideRepository
            .ReplaceAsync(target.Id, overrides, cancellationToken)
            .ConfigureAwait(false);

        await _unitOfWork
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}