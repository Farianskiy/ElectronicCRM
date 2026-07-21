using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog
    .CharacteristicDefinitions.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog
    .CharacteristicDefinitions.UpdateDefinition;

public sealed class
    UpdateCharacteristicDefinitionCommandHandler
{
    private readonly ICharacteristicDefinitionRepository
        _repository;

    public UpdateCharacteristicDefinitionCommandHandler(
        ICharacteristicDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        UpdateCharacteristicDefinitionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CharacteristicDefinitionId
            == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(
                        command
                            .CharacteristicDefinitionId)));
        }

        var definition = await _repository
            .GetByIdAsync(
                command.CharacteristicDefinitionId,
                cancellationToken)
            .ConfigureAwait(false);

        if (definition is null)
        {
            return UnitResult.Failure(
                CatalogErrors
                    .CharacteristicDefinitionNotFound(
                        command
                            .CharacteristicDefinitionId));
        }

        var updateResult = definition.UpdateDetails(
            command.Name,
            command.Unit);

        if (updateResult.IsFailure)
        {
            return UnitResult.Failure(
                updateResult.Error);
        }

        await _repository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}