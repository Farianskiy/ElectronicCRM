using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionMultiIntegerDraftRepository
{
    Task<Result<Guid, DomainError>> SaveAsync(
        CatalogRecognitionMultiIntegerDraft draft,
        IReadOnlyList<string> selectedProductNames,
        CancellationToken cancellationToken = default);
}