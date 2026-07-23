namespace ElectronicService.Core.Users.CreateManagerUser;

public sealed record CreateManagerUserCommand(
    string DisplayName,
    string Email,
    string Password);