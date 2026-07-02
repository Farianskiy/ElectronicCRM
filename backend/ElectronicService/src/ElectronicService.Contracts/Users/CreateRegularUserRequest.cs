namespace ElectronicService.Contracts.Users;

public sealed record CreateRegularUserRequest(
    string DisplayName,
    string? Email);