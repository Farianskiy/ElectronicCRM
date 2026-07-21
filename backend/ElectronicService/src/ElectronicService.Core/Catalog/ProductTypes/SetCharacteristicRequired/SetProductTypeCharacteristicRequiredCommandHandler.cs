using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ProductTypes
    .SetCharacteristicRequired;

public sealed class
    SetProductTypeCharacteristicRequiredCommandHandler
{
    private readonly IProductTypeSchemaRepository
        _repository;

    public SetProductTypeCharacteristicRequiredCommandHandler(
        IProductTypeSchemaRepository repository)
    {
        _repository = repository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        SetProductTypeCharacteristicRequiredCommand command,
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
         * ProductType загружается с tracking и вместе
         * с дочерними ProductTypeCharacteristic.
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
         * Операция идемпотентна.
         * Если состояние уже нужное, запись в БД
         * выполнять не требуется.
         */
        if (typeCharacteristic.IsRequired
            == command.IsRequired)
        {
            return UnitResult.Success<DomainError>();
        }

        /*
         * Повторная проверка выполняется только при
         * переходе false → true.
         *
         * Мы не доверяем значению CanMakeRequired,
         * ранее показанному frontend.
         */
        if (command.IsRequired)
        {
            var productsWithoutValueCount =
                await _repository
                    .CountProductsWithoutCharacteristicAsync(
                        productType.Id,
                        command
                            .CharacteristicDefinitionId,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (productsWithoutValueCount > 0)
            {
                return UnitResult.Failure(
                    CatalogErrors
                        .CharacteristicCannotBeMadeRequired(
                            command
                                .CharacteristicDefinitionId,
                            productsWithoutValueCount));
            }
        }

        var updateResult =
            productType.SetCharacteristicRequired(
                command.CharacteristicDefinitionId,
                command.IsRequired);

        if (updateResult.IsFailure)
        {
            return UnitResult.Failure(
                updateResult.Error);
        }

        /*
         * Между повторным COUNT и SaveChanges нет
         * пользовательского ввода или другого
         * прикладного действия.
         */
        await _repository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}