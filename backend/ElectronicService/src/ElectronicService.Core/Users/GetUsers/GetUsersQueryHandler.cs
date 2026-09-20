namespace ElectronicService.Core.Users.GetUsers;

public sealed class GetUsersQueryHandler
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<GetUsersResult> Handle(GetUsersQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = Math.Max(query.Page, 1);
        var pageSize = query.PageSize <= 0 ? 50 : Math.Clamp(query.PageSize, 1, 100);
        var (users, totalCount) = await _userRepository.GetPageAsync(query.Search, query.Type, query.Status, query.IncludeSystemDeveloper, page, pageSize, cancellationToken).ConfigureAwait(false);
        var items = users.Select(user => new GetUsersResultItem(user.Id, user.DisplayName.Value, user.Email?.Value, user.Type.ToString(), user.Status.ToString(), user.CreatedAtUtc)).ToArray();

        return new GetUsersResult(items, totalCount, page, pageSize);
    }
}
