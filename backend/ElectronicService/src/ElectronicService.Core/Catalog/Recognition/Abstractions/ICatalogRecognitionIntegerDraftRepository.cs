using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionIntegerDraftRepository
{
    Task<Result<Guid, DomainError>> SaveAsync(CatalogRecognitionIntegerDraft draft, CancellationToken cancellationToken = default);
}