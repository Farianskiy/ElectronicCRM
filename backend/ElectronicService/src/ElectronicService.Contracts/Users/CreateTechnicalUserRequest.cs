namespace ElectronicService.Contracts.Users;

public sealed class CreateTechnicalUserRequest
{
    public string DisplayName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}