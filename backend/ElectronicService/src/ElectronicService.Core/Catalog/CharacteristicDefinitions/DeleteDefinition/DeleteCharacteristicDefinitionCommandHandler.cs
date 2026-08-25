using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.CharacteristicDefinitions.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.CharacteristicDefinitions.DeleteDefinition;

public sealed class DeleteCharacteristicDefinitionCommandHandler
{
    private readonly ICharacteristicDefinitionRepository _repository;

    public DeleteCharacteristicDefinitionCommandHandler(ICharacteristicDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        DeleteCharacteristicDefinitionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CharacteristicDefinitionId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(command.CharacteristicDefinitionId)));
        }

        var definition = await _repository
            .GetByIdAsync(command.CharacteristicDefinitionId, cancellationToken)
            .ConfigureAwait(false);

        if (definition is null)
        {
            return UnitResult.Failure(
                CatalogErrors.CharacteristicDefinitionNotFound(command.CharacteristicDefinitionId));
        }

        var productTypesCount = await _repository
            .CountProductTypesUsingAsync(command.CharacteristicDefinitionId, cancellationToken)
            .ConfigureAwait(false);

        var productsWithValueCount = await _repository
            .CountProductsWithValueAsync(command.CharacteristicDefinitionId, cancellationToken)
            .ConfigureAwait(false);

        var importColumnsCount = await _repository
            .CountImportColumnsUsingAsync(command.CharacteristicDefinitionId, cancellationToken)
            .ConfigureAwait(false);

        if (productTypesCount > 0 || productsWithValueCount > 0 || importColumnsCount > 0)
        {
            return UnitResult.Failure(
                CatalogErrors.CharacteristicDefinitionCannotBeDeleted(
                    definition.Code,
                    productTypesCount,
                    productsWithValueCount,
                    importColumnsCount));
        }

        _repository.Remove(definition);

        await _repository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}