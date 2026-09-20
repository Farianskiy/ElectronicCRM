using System.Data;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionRuleSetVersionRepository
    : ICatalogRecognitionRuleSetVersionRepository
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogRecognitionRuleSetVersionRepository(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<Result<CatalogRecognitionRuleSetVersionSaveResult, DomainError>> CreateAsync(
        CatalogRecognitionRuleSetVersionSaveData data,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(data.Entries);

        var entries = data.Entries.ToArray();

        var domainData = new CatalogRecognitionRuleSetVersionData(
            data.ManufacturerId,
            data.ProductTypeId,
            1,
            data.Name,
            data.CreatedByUserId,
            entries);

        var validation = CatalogRecognitionRuleSetVersion.Create(domainData);

        if (validation.IsFailure)
        {
            return validation.Error;
        }

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var referencesValid = await ReferencesMatchScopeAsync(
                data.ManufacturerId,
                data.ProductTypeId,
                entries,
                cancellationToken).ConfigureAwait(false);

            if (!referencesValid)
            {
                return new DomainError(
                    "training.invalid_request",
                    "Один или несколько черновиков не найдены либо относятся к другому производителю или типу товара.");
            }

            var lastNumber = await _dbContext.CatalogRecognitionRuleSetVersions
                .AsNoTracking()
                .Where(version =>
                    version.ManufacturerId == data.ManufacturerId &&
                    version.ProductTypeId == data.ProductTypeId)
                .Select(version => (int?)version.VersionNumber)
                .MaxAsync(cancellationToken)
                .ConfigureAwait(false);

            if (lastNumber == int.MaxValue)
            {
                return new DomainError(
                    "training.conflict",
                    "Достигнут предел нумерации версий.");
            }

            var nextNumber = lastNumber.GetValueOrDefault() + 1;
            var creation = CatalogRecognitionRuleSetVersion.Create(
                domainData with { VersionNumber = nextNumber });

            if (creation.IsFailure)
            {
                return creation.Error;
            }

            var version = creation.Value;

            _dbContext.CatalogRecognitionRuleSetVersions.Add(version);

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return new CatalogRecognitionRuleSetVersionSaveResult(
                version.Id,
                version.VersionNumber);
        }
        catch (Exception exception) when (IsConcurrentWriteConflict(exception))
        {
            _dbContext.ChangeTracker.Clear();

            return new DomainError(
                "training.conflict",
                "Одновременно изменились данные или была создана другая версия. Обновите список версий и повторите создание.");
        }
    }

    private async Task<bool> ReferencesMatchScopeAsync(
        Guid manufacturerId,
        Guid productTypeId,
        CatalogRecognitionRuleSetEntryData[] entries,
        CancellationToken cancellationToken)
    {
        var literalIds = entries
            .Where(entry => entry.Kind == CatalogRecognitionRuleKind.Literal)
            .Select(entry => entry.DraftId)
            .ToArray();

        var numericIds = entries
            .Where(entry => entry.Kind == CatalogRecognitionRuleKind.NumericCapture)
            .Select(entry => entry.DraftId)
            .ToArray();

        var multipleNumericIds = entries
            .Where(entry => entry.Kind == CatalogRecognitionRuleKind.MultipleNumericCaptures)
            .Select(entry => entry.DraftId)
            .ToArray();

        if (literalIds.Length > 0)
        {
            var count = await _dbContext.CatalogRecognitionLiteralDrafts
                .AsNoTracking()
                .CountAsync(draft =>
                    literalIds.Contains(draft.Id) &&
                    draft.ManufacturerId == manufacturerId &&
                    draft.ProductTypeId == productTypeId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (count != literalIds.Length)
            {
                return false;
            }
        }

        if (numericIds.Length > 0)
        {
            var count = await _dbContext.CatalogRecognitionIntegerDrafts
                .AsNoTracking()
                .CountAsync(draft =>
                    numericIds.Contains(draft.Id) &&
                    draft.ManufacturerId == manufacturerId &&
                    draft.ProductTypeId == productTypeId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (count != numericIds.Length)
            {
                return false;
            }
        }

        if (multipleNumericIds.Length > 0)
        {
            var count = await _dbContext.CatalogRecognitionMultiIntegerDrafts
                .AsNoTracking()
                .CountAsync(draft =>
                    multipleNumericIds.Contains(draft.Id) &&
                    draft.ManufacturerId == manufacturerId &&
                    draft.ProductTypeId == productTypeId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (count != multipleNumericIds.Length)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsConcurrentWriteConflict(Exception exception)
    {
        var postgresException = exception as PostgresException
            ?? exception.InnerException as PostgresException;

        if (postgresException is null)
        {
            return false;
        }

        if (string.Equals(
            postgresException.SqlState,
            PostgresErrorCodes.SerializationFailure,
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
                "ux_rule_set_version_scope_number",
                StringComparison.Ordinal);
    }
}