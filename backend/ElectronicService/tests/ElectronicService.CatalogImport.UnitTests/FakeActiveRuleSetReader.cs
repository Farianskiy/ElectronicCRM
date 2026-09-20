using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Common;

namespace ElectronicService.CatalogImport.UnitTests;

internal sealed class FakeActiveRuleSetReader(CatalogRecognitionRuleSetExecutionSnapshot? snapshot = null)
    : ICatalogRecognitionActiveRuleSetReader
{
    public Task CaptureForRunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
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
