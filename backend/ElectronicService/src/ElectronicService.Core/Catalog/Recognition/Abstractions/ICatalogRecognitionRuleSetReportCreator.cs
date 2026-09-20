using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionRuleSetReportCreator
{
    Task<Result<CatalogRecognitionRuleSetReportCreated, DomainError>> CreateAsync(
        Guid versionId,
        Guid batchId,
        CancellationToken cancellationToken = default);
}