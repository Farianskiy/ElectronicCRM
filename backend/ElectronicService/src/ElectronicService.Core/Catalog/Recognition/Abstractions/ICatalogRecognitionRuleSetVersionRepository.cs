using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionRuleSetVersionRepository
{
    Task<Result<CatalogRecognitionRuleSetVersionSaveResult, DomainError>> CreateAsync(
        CatalogRecognitionRuleSetVersionSaveData data,
        CancellationToken cancellationToken = default);
}