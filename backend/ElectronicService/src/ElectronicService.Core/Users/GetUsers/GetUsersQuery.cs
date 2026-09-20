using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Users.GetUsers;

public sealed record GetUsersQuery(string? Search, UserType? Type, UserStatus? Status, bool IncludeSystemDeveloper, int Page, int PageSize);
