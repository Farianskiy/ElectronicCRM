using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionActiveRuleSetReader
{
    Task<Result<CatalogRecognitionRuleSetState, DomainError>> GetStateAsync(
        Guid manufacturerId,
        Guid productTypeId,
        CancellationToken cancellationToken = default);

    Task<Result<CatalogRecognitionActiveRuleSet, DomainError>> LoadAsync(
        Guid manufacturerId,
        Guid productTypeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogRecognitionRuleSetState>> CaptureForRunAsync(
        CancellationToken cancellationToken = default);
}