namespace ElectronicService.Core.Users.CreateTechnicalUser;

public sealed record CreateTechnicalUserCommand(
    string DisplayName,
    string Email);