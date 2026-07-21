using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ProductTypes
    .AddOptionalCharacteristic;

public sealed class
    AddOptionalCharacteristicToProductTypeCommandHandler
{
    private readonly IProductTypeSchemaRepository
        _repository;

    public AddOptionalCharacteristicToProductTypeCommandHandler(
        IProductTypeSchemaRepository repository)
    {
        _repository = repository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        AddOptionalCharacteristicToProductTypeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(
                command.ProductTypeCode))
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsRequired(
                    nameof(command.ProductTypeCode)));
        }

        if (command.CharacteristicDefinitionId
            == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(
                        command
                            .CharacteristicDefinitionId)));
        }

        var productType = await _repository
            .GetByCodeWithCharacteristicsAsync(
                command.ProductTypeCode,
                cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductTypeNotFound(
                    command.ProductTypeCode));
        }

        var definition = await _repository
            .GetDefinitionByIdAsync(
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

        /*
         * Настройки намеренно не принимаются из HTTP.
         * Новая характеристика всегда добавляется
         * в безопасном необязательном состоянии.
         */
        var addResult = productType.AddCharacteristic(
            definition,
            isRequired: false,
            isFilterable: false,
            isUsedForReplacement: false,
            replacementMatchMode:
                ReplacementMatchMode.None,
            replacementWeight: 0);

        if (addResult.IsFailure)
        {
            return UnitResult.Failure(
                addResult.Error);
        }

        await _repository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}