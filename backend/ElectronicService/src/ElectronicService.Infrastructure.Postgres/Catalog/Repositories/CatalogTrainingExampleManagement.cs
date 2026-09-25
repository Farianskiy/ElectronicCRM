using System.Data;
using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Core.Users;
using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Infrastructure.Postgres.Catalog.Queries;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogTrainingExampleManagement(ElectronicDbContext db,
    ICurrentUserProvider currentUser, IUserRepository users, IUserPermissionOverrideRepository permissions)
    : ICatalogTrainingExampleManagement
{
    public const string FormatVersion = "confirmed-examples-v1";
    public const string LegacyReason = "legacy-api: причина не передана старым клиентом";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private async Task<Result<Guid, DomainError>> AuthorizeAsync(CancellationToken ct)
    {
        if (currentUser.UserId is not Guid id || id == Guid.Empty)
            return new DomainError("training.unauthorized", "Необходимо войти в систему.");
        var user = await users.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (user is null || !user.IsActive)
            return new DomainError("training.forbidden", "Учётная запись недоступна.");
        var overrides = await permissions.GetByUserIdAsync(id, ct).ConfigureAwait(false);
        var allowed = overrides.SingleOrDefault(x => x.PermissionCode == UserPermissionCode.DictionariesManage)?.IsAllowed
            ?? UserPermissionCatalog.GetDefaults(user.Type).Contains(UserPermissionCode.DictionariesManage);
        if (!allowed) return new DomainError("training.forbidden", "Требуется право управления справочниками.");
        return id;
    }

    private static bool IsValid(TrainingExampleFilter filter) => filter.Page is >= 1 and <= 1000000 &&
        filter.Status is "active" or "revoked" or "sourceExcluded" or "all" && filter.ManufacturerId != Guid.Empty &&
        filter.ProductTypeId != Guid.Empty && filter.CharacteristicDefinitionId != Guid.Empty;

    private IQueryable<CatalogRecognitionTrainingExample> Owned(Guid id, TrainingExampleFilter filter) =>
        db.CatalogRecognitionTrainingExamples.AsNoTracking().Where(x => x.ConfirmedByUserId == id)
            .InScope(filter.ManufacturerId, filter.ProductTypeId, filter.CharacteristicDefinitionId);

    private IQueryable<TrainingExampleItem> Project(IQueryable<CatalogRecognitionTrainingExample> query) => query.Select(x =>
        new TrainingExampleItem(x.Id, x.SourceFeedbackId, x.ManufacturerId, x.ProductTypeId,
            x.CharacteristicDefinitionId, x.ProductName, x.RawValue, x.NormalizedValue, x.SpanStart,
            x.SpanLength, x.ConfirmedAtUtc, x.RevokedAtUtc, x.RevocationReason,
            db.CatalogRecognitionFeedbackEntries.Where(f => f.Id == x.SourceFeedbackId).Select(f => f.ImportBatchId).FirstOrDefault(),
            db.Manufacturers.Where(m => m.Id == x.ManufacturerId).Select(m => m.Name).FirstOrDefault() ?? "Производитель удалён",
            db.ProductTypes.Where(t => t.Id == x.ProductTypeId).Select(t => t.Name).FirstOrDefault() ?? "Тип удалён",
            db.CharacteristicDefinitions.Where(d => d.Id == x.CharacteristicDefinitionId).Select(d => d.Name).FirstOrDefault() ?? "Характеристика удалена",
            db.CatalogRecognitionFeedbackEntries.Where(f => f.Id == x.SourceFeedbackId).Select(f => f.ExcludedAtUtc).FirstOrDefault(),
            db.CatalogRecognitionFeedbackEntries.Where(f => f.Id == x.SourceFeedbackId).Select(f => f.ExclusionReason).FirstOrDefault(),
            db.CatalogRecognitionFeedbackEntries.Any(f => f.Id == x.SourceFeedbackId), x.IsEvaluationOnly));

    public async Task<Result<TrainingExamplePage, DomainError>> ListAsync(TrainingExampleFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var auth = await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
        if (auth.IsFailure) return auth.Error;
        if (!IsValid(filter)) return new DomainError("training.invalid_request", "Некорректные фильтры или страница.");
        var query = Owned(auth.Value, filter);
        if (string.Equals(filter.Status, "active", StringComparison.Ordinal)) query = query.ConfirmedExamples(db.CatalogRecognitionFeedbackEntries);
        if (string.Equals(filter.Status, "revoked", StringComparison.Ordinal)) query = query.Where(x => x.RevokedAtUtc != null);
        if (string.Equals(filter.Status, "sourceExcluded", StringComparison.Ordinal)) query = query.Where(x => db.CatalogRecognitionFeedbackEntries.Any(f => f.Id == x.SourceFeedbackId && f.ExcludedAtUtc != null));
        var items = await Project(query.OrderByDescending(x => x.ConfirmedAtUtc).ThenBy(x => x.Id)
                .Skip((filter.Page - 1) * 25).Take(26)).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        return new TrainingExamplePage(items.Take(25).ToArray(), filter.Page, items.Length > 25);
    }

    public async Task<Result<TrainingExampleItem, DomainError>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var auth = await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
        if (auth.IsFailure) return auth.Error;
        var item = await Project(Owned(auth.Value, new()).Where(x => x.Id == id))
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return item is null ? new DomainError("training.not_found", "Пример не найден или недоступен.") : item;
    }

    public async Task<UnitResult<DomainError>> SetPurposeAsync(Guid id, bool evaluationOnly, CancellationToken cancellationToken)
    {
        await using var gate = await new ElectronicService.Infrastructure.Postgres.Catalog.Recognition.Learning.RecognitionMutationGate(db)
            .EnterAsync(cancellationToken).ConfigureAwait(false);
        var auth = await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
        if (auth.IsFailure) return UnitResult.Failure(auth.Error);
        var changed = await db.CatalogRecognitionTrainingExamples.Where(x => x.Id == id && x.ConfirmedByUserId == auth.Value && x.RevokedAtUtc == null &&
            db.CatalogRecognitionFeedbackEntries.Any(f => f.Id == x.SourceFeedbackId && f.ExcludedAtUtc == null))
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsEvaluationOnly, evaluationOnly), cancellationToken).ConfigureAwait(false);
        return changed == 1 ? UnitResult.Success<DomainError>() : UnitResult.Failure(new DomainError("training.not_found", "Активный собственный пример не найден."));
    }

    public async Task<UnitResult<DomainError>> RevokeAsync(Guid id, string reason, CancellationToken cancellationToken)
    {
        var auth = await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
        if (auth.IsFailure) return UnitResult.Failure(auth.Error);
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000)
            return UnitResult.Failure(new DomainError("training.invalid_request", "Укажите причину отзыва (до 1000 символов)."));
        var owned = db.CatalogRecognitionTrainingExamples.Where(x => x.Id == id && x.ConfirmedByUserId == auth.Value);
        if (!await owned.AnyAsync(cancellationToken).ConfigureAwait(false))
            return UnitResult.Failure(new DomainError("training.not_found", "Пример не найден или недоступен."));
        var now = DateTime.UtcNow;
        var trimmed = reason.Trim();
        await owned.Where(x => x.RevokedAtUtc == null).ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.RevokedAtUtc, (DateTime?)now)
            .SetProperty(x => x.RevokedByUserId, (Guid?)auth.Value)
            .SetProperty(x => x.RevocationReason, trimmed), cancellationToken).ConfigureAwait(false);
        return UnitResult.Success<DomainError>();
    }

    public async Task<Result<ConfirmedExampleExportMetadata, DomainError>> ExportAsync(
        TrainingExampleFilter filter, Stream destination, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(destination);
        var auth = await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
        if (auth.IsFailure) return auth.Error;
        if (!IsValid(filter)) return new DomainError("training.invalid_request", "Некорректная область экспорта.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken).ConfigureAwait(false);
        // This SQL statement establishes the transaction snapshot before streaming any records.
        var selectionAsOfUtc = await db.Database.SqlQuery<DateTime>($"SELECT transaction_timestamp() AS \"Value\"")
            .SingleAsync(cancellationToken).ConfigureAwait(false);
        await using var writer = new StreamWriter(destination, new UTF8Encoding(false), 65536, leaveOpen: true);
        await writer.WriteLineAsync(JsonSerializer.Serialize(new
        {
            recordType = "metadata", formatVersion = FormatVersion, selectionAsOfUtc,
            spanUnits = "UTF-16 code units", selection = "current-active-confirmations-owned-by-current-user",
            scope = new { filter.ManufacturerId, filter.ProductTypeId, filter.CharacteristicDefinitionId }
        }, JsonOptions).AsMemory(), cancellationToken).ConfigureAwait(false);
        long count = 0;
        var query = Owned(auth.Value, filter).ConfirmedExamples(db.CatalogRecognitionFeedbackEntries).Where(x => !x.IsEvaluationOnly).OrderBy(x => x.ConfirmedAtUtc).ThenBy(x => x.Id);
        await foreach (var x in query.AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var line = JsonSerializer.Serialize(new
            {
                recordType = "example", formatVersion = FormatVersion, exampleId = x.Id,
                x.SourceFeedbackId, x.ManufacturerId, x.ProductTypeId, x.CharacteristicDefinitionId,
                x.ProductName, x.RawValue, x.NormalizedValue, x.SpanStart, x.SpanLength, x.ConfirmedAtUtc
            }, JsonOptions);
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
            count++;
        }
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new ConfirmedExampleExportMetadata(selectionAsOfUtc, count);
    }
}
