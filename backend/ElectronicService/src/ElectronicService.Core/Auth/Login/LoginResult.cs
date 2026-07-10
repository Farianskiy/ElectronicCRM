namespace ElectronicService.Core.Auth.Login;

public sealed record LoginResult(
    string AccessToken,
    Guid UserId,
    string UserType,
    string DisplayName);