using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionActiveRuleSetReader
    : ICatalogRecognitionActiveRuleSetReader
{
    private readonly ElectronicDbContext _dbContext;
    private readonly ICatalogRecognitionRuleSetExecutionReader _executionReader;
    private Dictionary<(Guid ManufacturerId, Guid ProductTypeId), CatalogRecognitionRuleSetState>? _capturedStates;

    public CatalogRecognitionActiveRuleSetReader(
        ElectronicDbContext dbContext,
        ICatalogRecognitionRuleSetExecutionReader executionReader)
    {
        _dbContext = dbContext;
        _executionReader = executionReader;
    }

    public async Task<Result<CatalogRecognitionRuleSetState, DomainError>>
        GetStateAsync(
            Guid manufacturerId,
            Guid productTypeId,
            CancellationToken cancellationToken = default)
    {
        if (manufacturerId == Guid.Empty || productTypeId == Guid.Empty)
        {
            return new DomainError(
                "training.invalid_request",
                "Укажите производителя и тип товара.");
        }

        var latest = await _dbContext.CatalogRecognitionRuleSetSwitches
            .AsNoTracking()
            .Where(item =>
                item.ManufacturerId == manufacturerId &&
                item.ProductTypeId == productTypeId)
            .OrderByDescending(item => item.SequenceNumber)
            .Select(item => new
            {
                item.Id,
                item.SequenceNumber,
                item.NewVersionId,
                item.ReportId,
                item.CreatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (latest is null)
        {
            return new CatalogRecognitionRuleSetState(
                manufacturerId,
                productTypeId,
                0,
                null,
                null,
                null,
                null);
        }

        return new CatalogRecognitionRuleSetState(
            manufacturerId,
            productTypeId,
            latest.SequenceNumber,
            latest.Id,
            latest.NewVersionId,
            latest.ReportId,
            latest.CreatedAtUtc);
    }

    public async Task<Result<CatalogRecognitionActiveRuleSet, DomainError>>
        LoadAsync(
            Guid manufacturerId,
            Guid productTypeId,
            CancellationToken cancellationToken = default)
    {
        if (manufacturerId == Guid.Empty || productTypeId == Guid.Empty)
        {
            return new DomainError(
                "training.invalid_request",
                "Укажите производителя и тип товара.");
        }

        Result<CatalogRecognitionRuleSetState, DomainError> stateResult;

        if (_capturedStates is null)
        {
            stateResult = await GetStateAsync(
                manufacturerId,
                productTypeId,
                cancellationToken).ConfigureAwait(false);
        }
        else if (_capturedStates.TryGetValue(
                     (manufacturerId, productTypeId),
                     out var captured))
        {
            stateResult = captured;
        }
        else
        {
            // На момент фиксации в этой области переключений не было.
            // Новую активацию во время обработки не подхватываем.
            stateResult = new CatalogRecognitionRuleSetState(
                manufacturerId,
                productTypeId,
                0,
                null,
                null,
                null,
                null);
        }

        if (stateResult.IsFailure)
        {
            return stateResult.Error;
        }

        var state = stateResult.Value;

        if (state.ActiveVersionId is not Guid versionId)
        {
            return new CatalogRecognitionActiveRuleSet(state, null);
        }

        var loaded = await _executionReader.ReadAsync(
            versionId,
            cancellationToken).ConfigureAwait(false);

        if (loaded.IsFailure)
        {
            return loaded.Error;
        }

        var snapshot = loaded.Value;

        if (snapshot.VersionId != versionId ||
            snapshot.ManufacturerId != manufacturerId ||
            snapshot.ProductTypeId != productTypeId)
        {
            return new DomainError(
                "training.invalid_data",
                "Активная версия не соответствует области распознавания.");
        }

        return new CatalogRecognitionActiveRuleSet(state, snapshot);
    }

    public async Task<IReadOnlyCollection<CatalogRecognitionRuleSetState>> CaptureForRunAsync(
    CancellationToken cancellationToken = default)
    {
        // Сбрасываем предыдущий снимок, если экземпляр используется повторно.
        _capturedStates = null;

        // Один SQL-запрос: последнее переключение каждой области.
        // Не исключаем отключения с NewVersionId == null.
        var latest = await _dbContext.CatalogRecognitionRuleSetSwitches
            .AsNoTracking()
            .Where(item =>
                !_dbContext.CatalogRecognitionRuleSetSwitches.Any(next =>
                    next.ManufacturerId == item.ManufacturerId &&
                    next.ProductTypeId == item.ProductTypeId &&
                    next.SequenceNumber > item.SequenceNumber))
            .Select(item => new
            {
                item.ManufacturerId,
                item.ProductTypeId,
                item.SequenceNumber,
                item.Id,
                item.NewVersionId,
                item.ReportId,
                item.CreatedAtUtc
            })
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        _capturedStates = latest.ToDictionary(
            item => (item.ManufacturerId, item.ProductTypeId),
            item => new CatalogRecognitionRuleSetState(
                item.ManufacturerId,
                item.ProductTypeId,
                item.SequenceNumber,
                item.Id,
                item.NewVersionId,
                item.ReportId,
                item.CreatedAtUtc));

        return _capturedStates.Values.ToArray();
    }
}