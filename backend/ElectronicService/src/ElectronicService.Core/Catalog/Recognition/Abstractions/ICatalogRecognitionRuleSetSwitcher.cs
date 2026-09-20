using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionRuleSetSwitcher
{
    Task<Result<CatalogRecognitionRuleSetSwitchResult, DomainError>> SwitchAsync(
        CatalogRecognitionRuleSetSwitchCommand command,
        CancellationToken cancellationToken = default);
}