using ElectronicService.Core.Catalog.Recognition.Effective;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Common;

namespace ElectronicService.CatalogImport.UnitTests;

internal sealed class FakeActiveRuleSetReader(CatalogRecognitionRuleSetExecutionSnapshot? snapshot = null)
    : ICatalogRecognitionActiveRuleSetReader, ICatalogRecognitionRuleSetExecutionReader
{
    public Task<IReadOnlyCollection<CatalogRecognitionRuleSetState>> CaptureForRunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<CatalogRecognitionRuleSetState> states = snapshot is null ? [] :
            [new(snapshot.ManufacturerId, snapshot.ProductTypeId, 1, null, snapshot.VersionId, null, null)];
        return Task.FromResult(states);
    }

    public Task<Result<CatalogRecognitionRuleSetExecutionSnapshot, DomainError>> ReadAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(snapshot is not null && snapshot.VersionId == versionId
            ? Result.Success<CatalogRecognitionRuleSetExecutionSnapshot, DomainError>(snapshot)
            : Result.Failure<CatalogRecognitionRuleSetExecutionSnapshot, DomainError>(new DomainError("training.not_found", "Missing test version")));
    }

    public static CatalogEffectiveRecognitionService Effective(ICatalogProductNameRecognitionService baseline, CatalogRecognitionRuleSetExecutionSnapshot? rules = null)
    {
        var reader = new FakeActiveRuleSetReader(rules);
        return new CatalogEffectiveRecognitionService(baseline, reader, reader);
    }

    public Task<Result<CatalogRecognitionRuleSetState, DomainError>> GetStateAsync(
        Guid manufacturerId, Guid productTypeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Result.Success<CatalogRecognitionRuleSetState, DomainError>(
            CreateState(manufacturerId, productTypeId)));
    }

    public Task<Result<CatalogRecognitionActiveRuleSet, DomainError>> LoadAsync(
        Guid manufacturerId, Guid productTypeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var matchingSnapshot = snapshot?.ManufacturerId == manufacturerId && snapshot.ProductTypeId == productTypeId
            ? snapshot : null;
        return Task.FromResult(Result.Success<CatalogRecognitionActiveRuleSet, DomainError>(
            new CatalogRecognitionActiveRuleSet(CreateState(manufacturerId, productTypeId), matchingSnapshot)));
    }

    private static CatalogRecognitionRuleSetState CreateState(Guid manufacturerId, Guid productTypeId)
        => new(manufacturerId, productTypeId, 0, null, null, null, null);
}
