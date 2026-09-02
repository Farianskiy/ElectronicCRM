using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.Manufacturers.Abstractions;
using ElectronicService.Core.Catalog.Manufacturers.Management;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Manufacturers.CreateApprovedAlias;

public sealed class CreateApprovedManufacturerAliasCommandHandler
{
    private readonly IManufacturerAliasRepository _manufacturerAliasRepository;
    private readonly IManufacturerNoisePhraseRepository _manufacturerNoisePhraseRepository;
    private readonly CatalogManufacturerPermissionChecker _permissionChecker;
    private readonly IUnitOfWork _unitOfWork;

    public CreateApprovedManufacturerAliasCommandHandler(
        IManufacturerAliasRepository manufacturerAliasRepository,
        IManufacturerNoisePhraseRepository manufacturerNoisePhraseRepository,
        CatalogManufacturerPermissionChecker permissionChecker,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(manufacturerAliasRepository);
        ArgumentNullException.ThrowIfNull(manufacturerNoisePhraseRepository);
        ArgumentNullException.ThrowIfNull(permissionChecker);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _manufacturerAliasRepository = manufacturerAliasRepository;
        _manufacturerNoisePhraseRepository = manufacturerNoisePhraseRepository;
        _permissionChecker = permissionChecker;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateApprovedManufacturerAliasResult, DomainError>> Handle(
        CreateApprovedManufacturerAliasCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var permissionResult = await _permissionChecker.EnsureCanManageAsync(cancellationToken).ConfigureAwait(false);

        if (permissionResult.IsFailure)
        {
            return permissionResult.Error;
        }

        if (command.ManufacturerId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(command.ManufacturerId));
        }

        var manufacturer = await _manufacturerAliasRepository
            .GetManufacturerByIdAsync(command.ManufacturerId, cancellationToken)
            .ConfigureAwait(false);

        if (manufacturer is null)
        {
            return CatalogErrors.ManufacturerNotFound(command.ManufacturerId);
        }

        var manufacturerAliasResult = ManufacturerAlias.Create(
            manufacturer.Id,
            command.Phrase,
            ManufacturerAliasStatus.Approved,
            ManufacturerAliasSource.TechnicalUser);

        if (manufacturerAliasResult.IsFailure)
        {
            return manufacturerAliasResult.Error;
        }

        var manufacturerAlias = manufacturerAliasResult.Value;

        var conflictsWithManufacturerName = await _manufacturerAliasRepository
            .ManufacturerNameExistsAsync(manufacturerAlias.NormalizedPhrase, cancellationToken)
            .ConfigureAwait(false);

        if (conflictsWithManufacturerName)
        {
            return ManufacturerAliasErrors.AliasConflictsWithManufacturerName(manufacturerAlias.NormalizedPhrase);
        }

        var activeAliasAlreadyExists = await _manufacturerAliasRepository
            .ActiveAliasExistsAsync(manufacturerAlias.NormalizedPhrase, cancellationToken)
            .ConfigureAwait(false);

        if (activeAliasAlreadyExists)
        {
            return ManufacturerAliasErrors.AliasAlreadyExists(manufacturerAlias.NormalizedPhrase);
        }

        var conflictsWithNoisePhrase = await _manufacturerNoisePhraseRepository
            .ActiveExistsAsync(manufacturerAlias.NormalizedPhrase, cancellationToken)
            .ConfigureAwait(false);

        if (conflictsWithNoisePhrase)
        {
            return ManufacturerAliasErrors.AliasConflictsWithNoisePhrase(manufacturerAlias.NormalizedPhrase);
        }

        _manufacturerAliasRepository.Add(manufacturerAlias);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateApprovedManufacturerAliasResult(
            manufacturerAlias.Id,
            manufacturer.Id,
            manufacturer.Name,
            manufacturerAlias.Phrase,
            manufacturerAlias.NormalizedPhrase,
            manufacturerAlias.Status.ToString(),
            manufacturerAlias.Source.ToString());
    }
}