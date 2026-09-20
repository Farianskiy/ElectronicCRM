using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionRuleSetReportReader
{
    Task<Result<CatalogRecognitionRuleSetReportPage, DomainError>> GetPageAsync(
        Guid reportId,
        int page,
        CancellationToken cancellationToken = default);

    Task<Result<
    IReadOnlyList<CatalogRecognitionRuleSetReportCreated>, DomainError>> GetRecentAsync(
        Guid versionId,
        Guid batchId,
        CancellationToken cancellationToken = default);
}