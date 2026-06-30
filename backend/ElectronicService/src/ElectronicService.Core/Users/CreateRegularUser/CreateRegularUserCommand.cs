namespace ElectronicService.Core.Users.CreateRegularUser;

public sealed record CreateRegularUserCommand(
    string DisplayName,
    string? Email);