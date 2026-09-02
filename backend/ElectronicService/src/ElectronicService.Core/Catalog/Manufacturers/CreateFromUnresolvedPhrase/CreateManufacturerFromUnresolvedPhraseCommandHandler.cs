using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.Manufacturers.Abstractions;
using ElectronicService.Core.Catalog.Manufacturers.Management;
using ElectronicService.Core.Catalog.Manufacturers.Resolution;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Manufacturers.CreateFromUnresolvedPhrase;

public sealed class CreateManufacturerFromUnresolvedPhraseCommandHandler
{
    private readonly IManufacturerRepository _manufacturerRepository;
    private readonly IManufacturerAliasRepository _manufacturerAliasRepository;
    private readonly CatalogManufacturerPermissionChecker _permissionChecker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IManufacturerNoisePhraseRepository _manufacturerNoisePhraseRepository;

    public CreateManufacturerFromUnresolvedPhraseCommandHandler(
        IManufacturerRepository manufacturerRepository,
        IManufacturerAliasRepository manufacturerAliasRepository,
        IManufacturerNoisePhraseRepository manufacturerNoisePhraseRepository,
        CatalogManufacturerPermissionChecker permissionChecker,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(manufacturerRepository);
        ArgumentNullException.ThrowIfNull(manufacturerAliasRepository);
        ArgumentNullException.ThrowIfNull(manufacturerNoisePhraseRepository);
        ArgumentNullException.ThrowIfNull(permissionChecker);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _manufacturerRepository = manufacturerRepository;
        _manufacturerAliasRepository = manufacturerAliasRepository;
        _manufacturerNoisePhraseRepository = manufacturerNoisePhraseRepository;
        _permissionChecker = permissionChecker;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateManufacturerFromUnresolvedPhraseResult, DomainError>> Handle(
        CreateManufacturerFromUnresolvedPhraseCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var permissionResult = await _permissionChecker.EnsureCanManageAsync(cancellationToken).ConfigureAwait(false);

        if (permissionResult.IsFailure)
        {
            return permissionResult.Error;
        }

        if (string.IsNullOrWhiteSpace(command.SourcePhrase))
        {
            return GeneralErrors.ValueIsRequired(nameof(command.SourcePhrase));
        }

        var trimmedSourcePhrase = command.SourcePhrase.Trim();

        if (trimmedSourcePhrase.Length > ManufacturerAlias.PhraseMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(command.SourcePhrase), ManufacturerAlias.PhraseMaxLength);
        }

        var manufacturerResult = Manufacturer.Create(command.CanonicalName);

        if (manufacturerResult.IsFailure)
        {
            return manufacturerResult.Error;
        }

        var manufacturer = manufacturerResult.Value;
        var normalizedSourcePhrase = ManufacturerNameNormalizer.Normalize(trimmedSourcePhrase);

        var manufacturerAlreadyExists = await _manufacturerRepository
            .ExistsByNormalizedNameAsync(manufacturer.NormalizedName, cancellationToken)
            .ConfigureAwait(false);

        if (manufacturerAlreadyExists)
        {
            return ManufacturerErrors.ManufacturerAlreadyExists(manufacturer.NormalizedName);
        }

        var manufacturerNameConflictsWithAlias = await _manufacturerAliasRepository
            .ActiveAliasExistsAsync(manufacturer.NormalizedName, cancellationToken)
            .ConfigureAwait(false);

        if (manufacturerNameConflictsWithAlias)
        {
            return ManufacturerErrors.ManufacturerNameConflictsWithAlias(manufacturer.NormalizedName);
        }

        var manufacturerNameConflictsWithNoisePhrase = await _manufacturerNoisePhraseRepository
            .ActiveExistsAsync(manufacturer.NormalizedName, cancellationToken)
            .ConfigureAwait(false);

        if (manufacturerNameConflictsWithNoisePhrase)
        {
            return ManufacturerErrors.ManufacturerNameConflictsWithNoisePhrase(manufacturer.NormalizedName);
        }

        ManufacturerAlias? manufacturerAlias = null;

        if (!string.Equals(normalizedSourcePhrase, manufacturer.NormalizedName, StringComparison.Ordinal))
        {
            var aliasConflictsWithManufacturerName = await _manufacturerRepository
                .ExistsByNormalizedNameAsync(normalizedSourcePhrase, cancellationToken)
                .ConfigureAwait(false);

            if (aliasConflictsWithManufacturerName)
            {
                return ManufacturerAliasErrors.AliasConflictsWithManufacturerName(normalizedSourcePhrase);
            }

            var activeAliasAlreadyExists = await _manufacturerAliasRepository
                .ActiveAliasExistsAsync(normalizedSourcePhrase, cancellationToken)
                .ConfigureAwait(false);

            if (activeAliasAlreadyExists)
            {
                return ManufacturerAliasErrors.AliasAlreadyExists(normalizedSourcePhrase);
            }

            var aliasConflictsWithNoisePhrase = await _manufacturerNoisePhraseRepository
            .ActiveExistsAsync(normalizedSourcePhrase, cancellationToken)
            .ConfigureAwait(false);

            if (aliasConflictsWithNoisePhrase)
            {
                return ManufacturerAliasErrors.AliasConflictsWithNoisePhrase(normalizedSourcePhrase);
            }

            var manufacturerAliasResult = ManufacturerAlias.Create(
                manufacturer.Id,
                trimmedSourcePhrase,
                ManufacturerAliasStatus.Approved,
                ManufacturerAliasSource.TechnicalUser);

            if (manufacturerAliasResult.IsFailure)
            {
                return manufacturerAliasResult.Error;
            }

            manufacturerAlias = manufacturerAliasResult.Value;
        }

        _manufacturerRepository.Add(manufacturer);

        if (manufacturerAlias is not null)
        {
            _manufacturerAliasRepository.Add(manufacturerAlias);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateManufacturerFromUnresolvedPhraseResult(
            manufacturer.Id,
            manufacturer.Name,
            manufacturer.NormalizedName,
            manufacturerAlias?.Id,
            manufacturerAlias?.Phrase,
            manufacturerAlias?.NormalizedPhrase,
            manufacturerAlias?.Status.ToString(),
            manufacturerAlias?.Source.ToString(),
            manufacturerAlias is null
                ? ManufacturerResolutionSource.ExactName.ToString()
                : ManufacturerResolutionSource.ApprovedAlias.ToString());
    }
}