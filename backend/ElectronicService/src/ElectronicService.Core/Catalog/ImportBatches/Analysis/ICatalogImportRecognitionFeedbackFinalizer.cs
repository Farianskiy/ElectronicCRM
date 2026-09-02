using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public interface ICatalogImportRecognitionFeedbackFinalizer
{
    Task<Result<int, DomainError>> FinalizeForAppliedBatchAsync(
        Guid importBatchId,
        Guid reviewedByUserId,
        UserType reviewerType,
        CancellationToken cancellationToken = default);
}