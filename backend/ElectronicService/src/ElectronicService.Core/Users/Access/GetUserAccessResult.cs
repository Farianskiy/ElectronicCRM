namespace ElectronicService.Core.Users.Access;

public sealed record GetUserAccessResult(
    Guid UserId,
    string UserType,
    IReadOnlyCollection<GetUserAccessResultItem> Permissions);

public sealed record GetUserAccessResultItem(
    string Code,
    string Group,
    string Label,
    bool IsAllowed,
    bool IsOverridden);