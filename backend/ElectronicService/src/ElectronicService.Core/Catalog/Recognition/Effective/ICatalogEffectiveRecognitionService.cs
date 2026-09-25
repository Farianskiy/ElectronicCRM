using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Effective;

public interface ICatalogEffectiveRecognitionService
{
    Task<CatalogRecognitionRunContext> CreateRunAsync(CancellationToken cancellationToken = default);

    Task<Result<CatalogEffectiveRecognitionResult, DomainError>> RecognizeAsync(
        CatalogEffectiveRecognitionRequest request, CancellationToken cancellationToken = default);
}
