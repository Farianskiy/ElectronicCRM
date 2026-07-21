using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ProductTypes
    .RemoveCharacteristic;

public sealed class
    RemoveCharacteristicFromProductTypeCommandHandler
{
    private readonly IProductTypeSchemaRepository
        _repository;

    public RemoveCharacteristicFromProductTypeCommandHandler(
        IProductTypeSchemaRepository repository)
    {
        _repository = repository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        RemoveCharacteristicFromProductTypeCommand command,
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

        /*
         * Загружаем tracked-агрегат вместе
         * с ProductTypeCharacteristic.
         */
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

        var typeCharacteristic =
            productType.Characteristics
                .FirstOrDefault(characteristic =>
                    characteristic
                        .CharacteristicDefinitionId
                    == command
                        .CharacteristicDefinitionId);

        if (typeCharacteristic is null)
        {
            return UnitResult.Failure(
                CatalogErrors
                    .ProductTypeCharacteristicNotFound(
                        productType.Id,
                        command
                            .CharacteristicDefinitionId));
        }

        /*
         * Повторная backend-проверка.
         *
         * Значению CanRemoveFromType, которое ранее
         * получил frontend, мы не доверяем.
         */
        var productsWithValueCount =
            await _repository
                .CountProductsWithCharacteristicAsync(
                    productType.Id,
                    command.CharacteristicDefinitionId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (productsWithValueCount > 0)
        {
            return UnitResult.Failure(
                CatalogErrors
                    .ProductTypeCharacteristicCannotBeRemoved(
                        productType.Id,
                        command
                            .CharacteristicDefinitionId,
                        productsWithValueCount));
        }

        /*
         * Сначала агрегат подтверждает изменение
         * своей внутренней коллекции.
         */
        var removeResult =
            productType.RemoveCharacteristic(
                command.CharacteristicDefinitionId);

        if (removeResult.IsFailure)
        {
            return UnitResult.Failure(
                removeResult.Error);
        }

        /*
         * Затем repository явно отмечает дочернюю
         * сущность как Deleted для EF Core.
         */
        _repository.MarkCharacteristicForRemoval(
            typeCharacteristic);

        await _repository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}