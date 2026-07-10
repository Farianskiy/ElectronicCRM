namespace ElectronicService.Contracts.Users;

public sealed class CreateRegularUserRequest
{
    public string DisplayName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string Password { get; init; } = string.Empty;
}