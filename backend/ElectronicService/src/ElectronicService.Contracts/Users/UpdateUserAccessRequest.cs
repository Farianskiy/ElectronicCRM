namespace ElectronicService.Contracts.Users;

public sealed class UpdateUserAccessRequest
{
    public string UserType { get; init; } = string.Empty;

    public IReadOnlyCollection<string> AllowedPermissions { get; init; } = [];
}