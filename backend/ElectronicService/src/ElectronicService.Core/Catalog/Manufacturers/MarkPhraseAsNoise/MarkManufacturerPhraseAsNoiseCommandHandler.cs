using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.Manufacturers.Abstractions;
using ElectronicService.Core.Catalog.Manufacturers.Management;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Manufacturers.MarkPhraseAsNoise;

public sealed class MarkManufacturerPhraseAsNoiseCommandHandler
{
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IManufacturerRepository _manufacturerRepository;
    private readonly IManufacturerAliasRepository _manufacturerAliasRepository;
    private readonly IManufacturerNoisePhraseRepository _manufacturerNoisePhraseRepository;
    private readonly CatalogManufacturerPermissionChecker _permissionChecker;
    private readonly IUnitOfWork _unitOfWork;

    public MarkManufacturerPhraseAsNoiseCommandHandler(
        ICurrentUserProvider currentUserProvider,
        IManufacturerRepository manufacturerRepository,
        IManufacturerAliasRepository manufacturerAliasRepository,
        IManufacturerNoisePhraseRepository manufacturerNoisePhraseRepository,
        CatalogManufacturerPermissionChecker permissionChecker,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(currentUserProvider);
        ArgumentNullException.ThrowIfNull(manufacturerRepository);
        ArgumentNullException.ThrowIfNull(manufacturerAliasRepository);
        ArgumentNullException.ThrowIfNull(manufacturerNoisePhraseRepository);
        ArgumentNullException.ThrowIfNull(permissionChecker);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _currentUserProvider = currentUserProvider;
        _manufacturerRepository = manufacturerRepository;
        _manufacturerAliasRepository = manufacturerAliasRepository;
        _manufacturerNoisePhraseRepository = manufacturerNoisePhraseRepository;
        _permissionChecker = permissionChecker;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MarkManufacturerPhraseAsNoiseResult, DomainError>> Handle(
        MarkManufacturerPhraseAsNoiseCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var permissionResult = await _permissionChecker.EnsureCanManageAsync(cancellationToken).ConfigureAwait(false);

        if (permissionResult.IsFailure)
        {
            return permissionResult.Error;
        }

        var currentUserId = _currentUserProvider.UserId;

        if (!currentUserId.HasValue)
        {
            return CatalogErrors.CurrentUserIsRequired();
        }

        var candidateResult = ManufacturerNoisePhrase.Create(
            command.Phrase,
            command.Reason,
            currentUserId.Value);

        if (candidateResult.IsFailure)
        {
            return candidateResult.Error;
        }

        var candidate = candidateResult.Value;

        var conflictsWithManufacturerName = await _manufacturerRepository
            .ExistsByNormalizedNameAsync(candidate.NormalizedPhrase, cancellationToken)
            .ConfigureAwait(false);

        if (conflictsWithManufacturerName)
        {
            return ManufacturerNoisePhraseErrors.ConflictsWithManufacturerName(candidate.NormalizedPhrase);
        }

        var conflictsWithManufacturerAlias = await _manufacturerAliasRepository
            .ActiveAliasExistsAsync(candidate.NormalizedPhrase, cancellationToken)
            .ConfigureAwait(false);

        if (conflictsWithManufacturerAlias)
        {
            return ManufacturerNoisePhraseErrors.ConflictsWithManufacturerAlias(candidate.NormalizedPhrase);
        }

        var existingNoisePhrase = await _manufacturerNoisePhraseRepository
            .GetByNormalizedPhraseAsync(candidate.NormalizedPhrase, cancellationToken)
            .ConfigureAwait(false);

        if (existingNoisePhrase is null)
        {
            _manufacturerNoisePhraseRepository.Add(candidate);

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return CreateResult(candidate, MarkManufacturerPhraseAsNoiseAction.Created);
        }

        if (existingNoisePhrase.IsActive)
        {
            return CreateResult(existingNoisePhrase, MarkManufacturerPhraseAsNoiseAction.AlreadyActive);
        }

        var updateReasonResult = existingNoisePhrase.UpdateReason(command.Reason, currentUserId.Value);

        if (updateReasonResult.IsFailure)
        {
            return updateReasonResult.Error;
        }

        var activateResult = existingNoisePhrase.Activate(currentUserId.Value);

        if (activateResult.IsFailure)
        {
            return activateResult.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CreateResult(existingNoisePhrase, MarkManufacturerPhraseAsNoiseAction.Reactivated);
    }

    private static MarkManufacturerPhraseAsNoiseResult CreateResult(
        ManufacturerNoisePhrase noisePhrase,
        MarkManufacturerPhraseAsNoiseAction action)
    {
        return new MarkManufacturerPhraseAsNoiseResult(
            noisePhrase.Id,
            noisePhrase.Phrase,
            noisePhrase.NormalizedPhrase,
            noisePhrase.Reason,
            noisePhrase.IsActive,
            noisePhrase.CreatedByUserId,
            noisePhrase.UpdatedByUserId,
            noisePhrase.CreatedAtUtc,
            noisePhrase.UpdatedAtUtc,
            noisePhrase.DeactivatedAtUtc,
            action);
    }
}