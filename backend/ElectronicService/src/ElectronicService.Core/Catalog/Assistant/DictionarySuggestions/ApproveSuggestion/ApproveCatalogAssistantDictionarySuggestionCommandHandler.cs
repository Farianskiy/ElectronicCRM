using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;
using ElectronicService.Core.Catalog.CharacteristicDefinitions.Abstractions;
using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Core.Catalog.ProductTypes.GetCharacteristicSchema;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;

public sealed class ApproveCatalogAssistantDictionarySuggestionCommandHandler
{
    private readonly ICatalogAssistantDictionarySuggestionRepository _suggestionRepository;
    private readonly ICatalogDictionaryRepository _dictionaryRepository;
    private readonly ICatalogRecognitionCandidateRepository _candidateRepository;
    private readonly ICharacteristicDefinitionRepository _characteristicDefinitionRepository;
    private readonly ICatalogProductTypeSchemaReader _productTypeSchemaReader;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public ApproveCatalogAssistantDictionarySuggestionCommandHandler(
        ICatalogAssistantDictionarySuggestionRepository suggestionRepository,
        ICatalogDictionaryRepository dictionaryRepository,
        ICatalogRecognitionCandidateRepository candidateRepository,
        ICharacteristicDefinitionRepository characteristicDefinitionRepository,
        ICatalogProductTypeSchemaReader productTypeSchemaReader,
        IUserRepository userRepository,
        ICurrentUserProvider currentUserProvider)
    {
        ArgumentNullException.ThrowIfNull(suggestionRepository);
        ArgumentNullException.ThrowIfNull(dictionaryRepository);
        ArgumentNullException.ThrowIfNull(candidateRepository);
        ArgumentNullException.ThrowIfNull(characteristicDefinitionRepository);
        ArgumentNullException.ThrowIfNull(productTypeSchemaReader);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(currentUserProvider);

        _suggestionRepository = suggestionRepository;
        _dictionaryRepository = dictionaryRepository;
        _candidateRepository = candidateRepository;
        _characteristicDefinitionRepository = characteristicDefinitionRepository;
        _productTypeSchemaReader = productTypeSchemaReader;
        _userRepository = userRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<UnitResult<DomainError>> Handle(ApproveCatalogAssistantDictionarySuggestionCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currentUserId = _currentUserProvider.UserId;

        if (!currentUserId.HasValue)
        {
            return UnitResult.Failure<DomainError>(CatalogErrors.CurrentUserIsRequired());
        }

        var user = await _userRepository.GetByIdAsync(currentUserId.Value, cancellationToken).ConfigureAwait(false);

        if (user is null || !user.CanManageProductSynonyms())
        {
            return UnitResult.Failure<DomainError>(CatalogErrors.UserCannotReviewDictionarySuggestion());
        }

        if (!Enum.TryParse<CatalogDictionaryTermKind>(command.Kind, ignoreCase: true, out var kind) || !Enum.IsDefined(kind) || kind == CatalogDictionaryTermKind.None)
        {
            return UnitResult.Failure<DomainError>(GeneralErrors.ValueIsInvalid(nameof(command.Kind)));
        }

        var suggestion = await _suggestionRepository.GetByIdAsync(command.SuggestionId, cancellationToken).ConfigureAwait(false);

        if (suggestion is null)
        {
            return UnitResult.Failure<DomainError>(CatalogErrors.DictionarySuggestionNotFound(command.SuggestionId.ToString()));
        }

        if (!suggestion.IsPending)
        {
            return UnitResult.Failure<DomainError>(GeneralErrors.ValueIsInvalid(nameof(suggestion.Status)));
        }

        CatalogRecognitionCandidate? candidate = null;

        if (suggestion.IsGeneratedFromRecognitionLearning)
        {
            candidate = await _candidateRepository.GetBySuggestionIdAsync(suggestion.Id, cancellationToken).ConfigureAwait(false);

            if (candidate is null)
            {
                return UnitResult.Failure<DomainError>(CatalogErrors.RecognitionCandidateForSuggestionNotFound(suggestion.Id));
            }

            if (!candidate.ManufacturerId.HasValue || candidate.ManufacturerId.Value == Guid.Empty || suggestion.ManufacturerId != candidate.ManufacturerId)
            {
                return UnitResult.Failure<DomainError>(GeneralErrors.ValueIsInvalid(nameof(suggestion.ManufacturerId)));
            }
        }

        CatalogProductTypeCharacteristicSchemaResult? productTypeSchema = null;

        if (!string.IsNullOrWhiteSpace(command.ProductTypeCode))
        {
            productTypeSchema = await _productTypeSchemaReader.GetByCodeAsync(command.ProductTypeCode.Trim(), cancellationToken).ConfigureAwait(false);

            if (productTypeSchema is null)
            {
                return UnitResult.Failure<DomainError>(CatalogErrors.ProductTypeNotFound(command.ProductTypeCode));
            }
        }

        var finalTargetCode = kind == CatalogDictionaryTermKind.Characteristic
            ? NormalizeCode(command.TargetCode)
            : null;

        Guid? characteristicDefinitionId = null;

        if (kind == CatalogDictionaryTermKind.Characteristic)
        {
            if (string.IsNullOrWhiteSpace(finalTargetCode))
            {
                return UnitResult.Failure<DomainError>(GeneralErrors.ValueIsInvalid(nameof(command.TargetCode)));
            }

            if (productTypeSchema is not null)
            {
                var schemaCharacteristic = productTypeSchema.Characteristics.FirstOrDefault(
                    characteristic => string.Equals(
                        NormalizeCode(characteristic.Code),
                        finalTargetCode,
                        StringComparison.Ordinal));

                if (schemaCharacteristic is null)
                {
                    return UnitResult.Failure<DomainError>(
                        CatalogErrors.DictionaryTargetCharacteristicIsNotAllowedForProductType(
                            finalTargetCode,
                            productTypeSchema.ProductTypeCode));
                }

                characteristicDefinitionId = schemaCharacteristic.DefinitionId;
            }
            else
            {
                var characteristicExists = await _characteristicDefinitionRepository.ExistsByCodeAsync(finalTargetCode, cancellationToken).ConfigureAwait(false);

                if (!characteristicExists)
                {
                    return UnitResult.Failure<DomainError>(CatalogErrors.CharacteristicDefinitionNotFound(finalTargetCode));
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(command.TargetCode))
        {
            return UnitResult.Failure<DomainError>(GeneralErrors.ValueIsInvalid(nameof(command.TargetCode)));
        }

        var decisionResult = CatalogDictionarySuggestionApprovalDecision.Create(
            command.Phrase,
            kind,
            finalTargetCode,
            command.TargetValue,
            suggestion.ManufacturerId,
            productTypeSchema?.ProductTypeId,
            characteristicDefinitionId,
            command.Priority);

        if (decisionResult.IsFailure)
        {
            return UnitResult.Failure<DomainError>(decisionResult.Error);
        }

        var decision = decisionResult.Value;

        var termResult = CatalogDictionaryTerm.Create(
            decision.Phrase,
            decision.Kind,
            decision.TargetCode,
            decision.TargetValue,
            decision.Priority,
            CatalogDictionaryTermStatus.Approved,
            CatalogDictionaryTermSource.UserCorrection,
            decision.ManufacturerId,
            decision.ProductTypeId);

        if (termResult.IsFailure)
        {
            return UnitResult.Failure<DomainError>(termResult.Error);
        }

        var term = termResult.Value;
        var termAlreadyExists = await _dictionaryRepository.ExistsAsync(term, cancellationToken).ConfigureAwait(false);

        if (termAlreadyExists)
        {
            return UnitResult.Failure<DomainError>(CatalogErrors.DictionaryTermAlreadyExists(decision.Phrase));
        }

        var approveResult = suggestion.ApproveWithDecision(
            decision,
            term.Id,
            currentUserId.Value,
            command.ReviewComment);

        if (approveResult.IsFailure)
        {
            return UnitResult.Failure<DomainError>(approveResult.Error);
        }

        if (candidate is not null)
        {
            var markCandidateResult = candidate.MarkApproved();

            if (markCandidateResult.IsFailure)
            {
                return UnitResult.Failure<DomainError>(markCandidateResult.Error);
            }
        }

        _dictionaryRepository.Add(term);

        await _suggestionRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }

    private static string NormalizeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal);
    }
}