namespace ElectronicService.Core.Auth.Login;

public sealed record LoginCommand(
    string Email,
    string Password);