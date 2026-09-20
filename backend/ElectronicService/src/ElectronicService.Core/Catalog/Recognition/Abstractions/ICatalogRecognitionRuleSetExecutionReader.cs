using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionRuleSetExecutionReader
{
    Task<Result<CatalogRecognitionRuleSetExecutionSnapshot, DomainError>> ReadAsync(
        Guid versionId,
        CancellationToken cancellationToken = default);
}