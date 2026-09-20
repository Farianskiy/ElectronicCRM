using System.Data;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Core.Users;
using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionRuleSetSwitcher
    : ICatalogRecognitionRuleSetSwitcher
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CatalogRecognitionRuleSetSwitcher(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<Result<CatalogRecognitionRuleSetSwitchResult, DomainError>>
        SwitchAsync(
            CatalogRecognitionRuleSetSwitchCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!command.Confirmed)
        {
            return Error(
                "training.invalid_request",
                "Необходимо явно подтвердить переключение.");
        }

        if (command.ManufacturerId == Guid.Empty ||
            command.ProductTypeId == Guid.Empty ||
            command.ExpectedSequenceNumber < 0 ||
            command.ExpectedSequenceNumber == long.MaxValue ||
            command.NewVersionId == Guid.Empty ||
            command.ReportId == Guid.Empty ||
            command.NewVersionId.HasValue != command.ReportId.HasValue ||
            string.IsNullOrWhiteSpace(command.Reason) ||
            command.Reason.Length > 1000)
        {
            return Error(
                "training.invalid_request",
                "Проверьте область, номер переключения, версию, отчёт и причину.");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ElectronicDbContext>();

        var currentUser = scope.ServiceProvider
            .GetRequiredService<ICurrentUserProvider>();

        if (currentUser.UserId is not Guid userId || userId == Guid.Empty)
        {
            return Error(
                "training.unauthorized",
                "Необходимо войти в систему.");
        }

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var users = scope.ServiceProvider
                .GetRequiredService<IUserRepository>();

            var permissions = scope.ServiceProvider
                .GetRequiredService<IUserPermissionOverrideRepository>();

            var user = await users.GetByIdAsync(
                userId,
                cancellationToken).ConfigureAwait(false);

            if (user is null || !user.IsActive)
            {
                return Error(
                    "training.forbidden",
                    "Учётная запись недоступна.");
            }

            var overrides = await permissions.GetByUserIdAsync(
                userId,
                cancellationToken).ConfigureAwait(false);

            var permissionOverride = overrides.SingleOrDefault(item =>
                item.PermissionCode == UserPermissionCode.DictionariesManage);

            var allowed = permissionOverride?.IsAllowed ??
                UserPermissionCatalog.GetDefaults(user.Type)
                    .Contains(UserPermissionCode.DictionariesManage);

            if (!allowed)
            {
                return Error(
                    "training.forbidden",
                    "Требуется право управления справочниками.");
            }

            var previous = await dbContext.CatalogRecognitionRuleSetSwitches
                .AsNoTracking()
                .Where(item =>
                    item.ManufacturerId == command.ManufacturerId &&
                    item.ProductTypeId == command.ProductTypeId)
                .OrderByDescending(item => item.SequenceNumber)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var currentSequence = previous?.SequenceNumber ?? 0;
            var activeVersionId = previous?.NewVersionId;

            if (currentSequence != command.ExpectedSequenceNumber)
            {
                return Error(
                    "training.conflict",
                    "Состояние уже изменилось. Получите актуальное состояние перед новым переключением.");
            }

            if (activeVersionId == command.NewVersionId)
            {
                return Error(
                    "training.conflict",
                    "Запрошенное состояние уже установлено.");
            }

            if (command.NewVersionId is Guid newVersionId)
            {
                var version = await dbContext.CatalogRecognitionRuleSetVersions
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item => item.Id == newVersionId,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (version is null)
                {
                    return Error(
                        "training.not_found",
                        "Версия правил не найдена.");
                }

                if (version.ManufacturerId != command.ManufacturerId ||
                    version.ProductTypeId != command.ProductTypeId)
                {
                    return Error(
                        "training.scope_mismatch",
                        "Версия относится к другому производителю или типу товара.");
                }

                var validator = scope.ServiceProvider
                    .GetRequiredService<CatalogRecognitionRuleSetActivationValidator>();

                var validation = await validator.ValidateAsync(
                    newVersionId,
                    command.ReportId.GetValueOrDefault(),
                    cancellationToken).ConfigureAwait(false);

                if (validation.IsFailure)
                {
                    return validation.Error;
                }
            }

            var creation = CatalogRecognitionRuleSetSwitch.Create(
                new CatalogRecognitionRuleSetSwitchData(
                    command.ManufacturerId,
                    command.ProductTypeId,
                    currentSequence + 1,
                    activeVersionId,
                    command.NewVersionId,
                    command.ReportId,
                    userId,
                    command.Reason));

            if (creation.IsFailure)
            {
                return creation.Error;
            }

            var change = creation.Value;

            dbContext.CatalogRecognitionRuleSetSwitches.Add(change);

            await dbContext.SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken)
                .ConfigureAwait(false);

            return new CatalogRecognitionRuleSetSwitchResult(
                change.Id,
                change.SequenceNumber,
                change.PreviousVersionId,
                change.NewVersionId,
                change.CreatedAtUtc);
        }
        catch (Exception exception) when (IsConcurrentConflict(exception))
        {
            return Error(
                "training.conflict",
                "Данные изменились одновременно с переключением. Обновите состояние и повторно подтвердите действие.");
        }
    }

    private static bool IsConcurrentConflict(Exception exception)
    {
        var postgresException = exception as PostgresException ??
            exception.InnerException as PostgresException;

        if (postgresException is null)
        {
            return false;
        }

        if (string.Equals(
                postgresException.SqlState,
                PostgresErrorCodes.SerializationFailure,
                StringComparison.Ordinal) ||
            string.Equals(
                postgresException.SqlState,
                PostgresErrorCodes.DeadlockDetected,
                StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(
                postgresException.SqlState,
                PostgresErrorCodes.UniqueViolation,
                StringComparison.Ordinal) &&
            string.Equals(
                postgresException.ConstraintName,
                "ux_rule_set_switch_scope_sequence",
                StringComparison.Ordinal);
    }

    private static DomainError Error(string code, string message)
    {
        return new DomainError(code, message);
    }
}