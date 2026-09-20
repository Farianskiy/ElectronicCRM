using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionLiteralDraftRepository
{
    Task<Result<Guid, DomainError>> SaveAsync(CatalogRecognitionLiteralDraft draft, CancellationToken cancellationToken = default);
}