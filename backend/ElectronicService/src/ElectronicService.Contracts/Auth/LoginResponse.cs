namespace ElectronicService.Contracts.Auth;

public sealed record LoginResponse(
    string AccessToken,
    Guid UserId,
    string UserType,
    string DisplayName);