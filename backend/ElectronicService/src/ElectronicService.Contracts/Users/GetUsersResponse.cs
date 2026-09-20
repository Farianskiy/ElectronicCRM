namespace ElectronicService.Contracts.Users;

public sealed record GetUsersResponse(IReadOnlyCollection<GetUsersResponseItem> Items, int TotalCount, int Page, int PageSize);

public sealed record GetUsersResponseItem(Guid Id, string DisplayName, string? Email, string UserType, string Status, DateTime CreatedAtUtc);
