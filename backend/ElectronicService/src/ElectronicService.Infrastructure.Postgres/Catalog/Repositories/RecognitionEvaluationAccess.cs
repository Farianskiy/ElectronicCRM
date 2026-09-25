using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class RecognitionEvaluationAccess(ICurrentUserProvider currentUser, IUserRepository users, IUserPermissionOverrideRepository permissions)
{
    public async Task<Result<Guid, DomainError>> AuthorizeAsync(CancellationToken ct)
    {
        if (currentUser.UserId is not Guid id || id == Guid.Empty) return new DomainError("training.unauthorized", "Необходимо войти в систему.");
        var user = await users.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (user is null || !user.IsActive) return new DomainError("training.forbidden", "Учётная запись недоступна.");
        var overrides = await permissions.GetByUserIdAsync(id, ct).ConfigureAwait(false);
        var allowed = overrides.SingleOrDefault(x => x.PermissionCode == UserPermissionCode.DictionariesManage)?.IsAllowed
            ?? UserPermissionCatalog.GetDefaults(user.Type).Contains(UserPermissionCode.DictionariesManage);
        return allowed ? id : new DomainError("training.forbidden", "Требуется право управления справочниками.");
    }
}
