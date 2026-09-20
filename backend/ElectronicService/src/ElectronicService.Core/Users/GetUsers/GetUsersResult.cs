namespace ElectronicService.Core.Users.GetUsers;

public sealed record GetUsersResult(IReadOnlyCollection<GetUsersResultItem> Items, int TotalCount, int Page, int PageSize);

public sealed record GetUsersResultItem(Guid Id, string DisplayName, string? Email, string UserType, string Status, DateTime CreatedAtUtc);
