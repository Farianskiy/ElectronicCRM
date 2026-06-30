namespace ElectronicService.Core.Users.MakeUserTechnical;

public sealed record MakeUserTechnicalCommand(
    Guid UserId,
    string Email);