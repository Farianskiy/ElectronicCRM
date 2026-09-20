namespace ElectronicService.Core.Users.CreateAdministratorUser;

public sealed record CreateAdministratorUserCommand(string DisplayName, string Email, string Password);