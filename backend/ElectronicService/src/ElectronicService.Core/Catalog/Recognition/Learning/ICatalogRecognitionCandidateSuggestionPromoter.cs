using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Learning;

public interface ICatalogRecognitionCandidateSuggestionPromoter
{
    Task<Result<CatalogRecognitionCandidateSuggestionPromotionResult, DomainError>> PromoteEligibleAsync(Guid upperId, Guid? after = null, int batchSize = 500, CancellationToken cancellationToken = default);
}