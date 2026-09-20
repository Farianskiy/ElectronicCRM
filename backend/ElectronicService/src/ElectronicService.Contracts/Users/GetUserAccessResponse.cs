namespace ElectronicService.Contracts.Users;

public sealed record GetUserAccessResponse(Guid UserId, string UserType, IReadOnlyCollection<GetUserAccessResponseItem> Permissions);

public sealed record GetUserAccessResponseItem(string Code, string Group, string Label, bool IsAllowed, bool IsOverridden);