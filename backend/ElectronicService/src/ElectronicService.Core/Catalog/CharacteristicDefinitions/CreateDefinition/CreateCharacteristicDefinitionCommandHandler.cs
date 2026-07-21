using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog
    .CharacteristicDefinitions.Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog
    .CharacteristicDefinitions.CreateDefinition;

public sealed class
    CreateCharacteristicDefinitionCommandHandler
{
    private readonly ICharacteristicDefinitionRepository
        _repository;

    public CreateCharacteristicDefinitionCommandHandler(
        ICharacteristicDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        CreateCharacteristicDefinitionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.DataType)
            || !Enum.TryParse<CharacteristicDataType>(
                command.DataType,
                ignoreCase: true,
                out var dataType)
            || dataType == CharacteristicDataType.None)
        {
            return Result.Failure<Guid, DomainError>(
                GeneralErrors.ValueIsInvalid(
                    nameof(command.DataType)));
        }

        /*
         * Домен нормализует код:
         * RATED-CURRENT → RATED_CURRENT
         * rated current → RATED_CURRENT
         */
        var createResult =
            CharacteristicDefinition.Create(
                command.Code,
                command.Name,
                dataType,
                command.Unit);

        if (createResult.IsFailure)
        {
            return Result.Failure<Guid, DomainError>(
                createResult.Error);
        }

        var definition = createResult.Value;

        var alreadyExists = await _repository
            .ExistsByCodeAsync(
                definition.Code,
                cancellationToken)
            .ConfigureAwait(false);

        if (alreadyExists)
        {
            return Result.Failure<Guid, DomainError>(
                CatalogErrors.CharacteristicAlreadyExists(
                    definition.Code));
        }

        _repository.Add(definition);

        await _repository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<Guid, DomainError>(
            definition.Id);
    }
}