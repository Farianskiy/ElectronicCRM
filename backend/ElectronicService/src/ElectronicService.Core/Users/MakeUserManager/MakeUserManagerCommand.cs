namespace ElectronicService.Core.Users.MakeUserManager;

public sealed record MakeUserManagerCommand(
    Guid UserId,
    string Email);