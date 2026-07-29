using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users;

namespace ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportBatchHistory;

public sealed class GetCatalogImportBatchHistoryQueryHandler
{
    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly IUserRepository _userRepository;

    public GetCatalogImportBatchHistoryQueryHandler(
        ICatalogImportBatchRepository importBatchRepository,
        IUserRepository userRepository)
    {
        _importBatchRepository = importBatchRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<GetCatalogImportBatchHistoryResult, DomainError>> Handle(
        GetCatalogImportBatchHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<GetCatalogImportBatchHistoryResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (query.BatchId == Guid.Empty)
        {
            return Result.Failure<GetCatalogImportBatchHistoryResult, DomainError>(
                CatalogImportErrors.BatchNotFound(query.BatchId));
        }

        var currentUser = await _userRepository
            .GetByIdAsync(
                query.CurrentUserId,
                cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<GetCatalogImportBatchHistoryResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        var batch = await _importBatchRepository
            .GetByIdAsync(
                query.BatchId,
                cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<GetCatalogImportBatchHistoryResult, DomainError>(
                CatalogImportErrors.BatchNotFound(query.BatchId));
        }

        var isOwner =
            batch.CreatedByUserId == currentUser.Id;

        var canRead =
            isOwner ||
            currentUser.CanReviewCatalogImports();

        if (!canRead)
        {
            return Result.Failure<GetCatalogImportBatchHistoryResult, DomainError>(
                CatalogImportErrors.UserCannotAccessBatch());
        }

        var actorUserIds = new HashSet<Guid>
        {
            batch.CreatedByUserId
        };

        AddUserId(
            actorUserIds,
            batch.ReviewedByUserId);

        AddUserId(
            actorUserIds,
            batch.ChangesRequestedByUserId);

        AddUserId(
            actorUserIds,
            batch.RejectedByUserId);

        AddUserId(
            actorUserIds,
            batch.AppliedByUserId);

        var users = await _userRepository
            .GetByIdsAsync(
                actorUserIds.ToArray(),
                cancellationToken)
            .ConfigureAwait(false);

        var usersById = users.ToDictionary(
            user => user.Id);

        var items =
            new List<CatalogImportBatchHistoryItemResult>();

        items.Add(
            CreateItem(
                eventType: "Uploaded",
                occurredAtUtc: batch.CreatedAtUtc,
                actorUserId: batch.CreatedByUserId,
                comment: $"Загружен файл «{batch.OriginalFileName}».",
                usersById));

        if (batch.SubmittedAtUtc.HasValue)
        {
            items.Add(
                CreateItem(
                    eventType: "Submitted",
                    occurredAtUtc: batch.SubmittedAtUtc.Value,
                    actorUserId: batch.CreatedByUserId,
                    comment: "Пакет отправлен на техническую проверку.",
                    usersById));
        }

        if (
            batch.ReviewedAtUtc.HasValue &&
            batch.ReviewedByUserId.HasValue
        )
        {
            items.Add(
                CreateItem(
                    eventType: "ReviewStarted",
                    occurredAtUtc: batch.ReviewedAtUtc.Value,
                    actorUserId: batch.ReviewedByUserId,
                    comment: "Технический специалист начал проверку.",
                    usersById));
        }

        if (batch.ChangesRequestedAtUtc.HasValue)
        {
            items.Add(
                CreateItem(
                    eventType: "ChangesRequested",
                    occurredAtUtc: batch.ChangesRequestedAtUtc.Value,
                    actorUserId: batch.ChangesRequestedByUserId,
                    comment: batch.ChangesRequestComment,
                    usersById));
        }

        if (batch.RejectedAtUtc.HasValue)
        {
            items.Add(
                CreateItem(
                    eventType: "Rejected",
                    occurredAtUtc: batch.RejectedAtUtc.Value,
                    actorUserId: batch.RejectedByUserId,
                    comment: batch.RejectionReason,
                    usersById));
        }

        if (batch.AppliedAtUtc.HasValue)
        {
            items.Add(
                CreateItem(
                    eventType: "Applied",
                    occurredAtUtc: batch.AppliedAtUtc.Value,
                    actorUserId: batch.AppliedByUserId,
                    comment:
                        $"Пакет применён. Создано товаров: {batch.ValidRowsCount}.",
                    usersById));
        }

        var orderedItems = items
            .OrderBy(item => item.OccurredAtUtc)
            .ToArray();

        var result =
            new GetCatalogImportBatchHistoryResult(
                batch.Id,
                orderedItems);

        return Result.Success<
            GetCatalogImportBatchHistoryResult,
            DomainError>(result);
    }

    private static void AddUserId(
        HashSet<Guid> ids,
        Guid? userId)
    {
        if (
            userId.HasValue &&
            userId.Value != Guid.Empty
        )
        {
            ids.Add(userId.Value);
        }
    }

    private static CatalogImportBatchHistoryItemResult CreateItem(
        string eventType,
        DateTime occurredAtUtc,
        Guid? actorUserId,
        string? comment,
        Dictionary<Guid, User> usersById)
    {
        User? actor = null;

        if (actorUserId.HasValue)
        {
            usersById.TryGetValue(
                actorUserId.Value,
                out actor);
        }

        return new CatalogImportBatchHistoryItemResult(
            eventType,
            occurredAtUtc,
            actorUserId,
            actor?.DisplayName.Value,
            actor?.Email?.Value,
            actor?.Type.ToString(),
            comment);
    }
}