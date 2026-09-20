using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Errors;

namespace ElectronicService.Core.Users.Access;

public sealed class GetUserAccessQueryHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionOverrideRepository _overrideRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetUserAccessQueryHandler(
        IUserRepository userRepository,
        IUserPermissionOverrideRepository overrideRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _userRepository = userRepository;
        _overrideRepository = overrideRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<GetUserAccessResult, DomainError>> Handle(
        GetUserAccessQuery query,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository
            .GetByIdAsync(query.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null ||
            user.IsSystemDeveloper &&
            _currentUserProvider.UserId != user.Id)
        {
            return UserErrors.NotFound(query.UserId);
        }

        var overrides = await _overrideRepository
            .GetByUserIdAsync(user.Id, cancellationToken)
            .ConfigureAwait(false);

        var overrideLookup = overrides.ToDictionary(
            value => value.PermissionCode,
            value => value.IsAllowed);

        var defaults = UserPermissionCatalog.GetDefaults(user.Type);

        var permissions = UserPermissionCatalog.Definitions
            .Select(definition => new GetUserAccessResultItem(
                definition.Code.ToString(),
                definition.Group,
                definition.Label,
                overrideLookup.GetValueOrDefault(
                    definition.Code,
                    defaults.Contains(definition.Code)),
                overrideLookup.ContainsKey(definition.Code)))
            .ToArray();

        return new GetUserAccessResult(
            user.Id,
            user.Type.ToString(),
            permissions);
    }
}