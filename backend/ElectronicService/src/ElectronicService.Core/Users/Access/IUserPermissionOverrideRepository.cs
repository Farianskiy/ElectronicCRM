using ElectronicService.Domain.Users;

namespace ElectronicService.Core.Users.Access;

public interface IUserPermissionOverrideRepository
{
    Task<IReadOnlyCollection<UserPermissionOverride>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task ReplaceAsync(
        Guid userId,
        IReadOnlyCollection<UserPermissionOverride> values,
        CancellationToken cancellationToken = default);
}