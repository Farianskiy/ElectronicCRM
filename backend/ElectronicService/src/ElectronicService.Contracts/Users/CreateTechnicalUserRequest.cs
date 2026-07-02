namespace ElectronicService.Contracts.Users;

public sealed record CreateTechnicalUserRequest(
    string DisplayName,
    string Email);