using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Core.Catalog.ProductTypes.GetCharacteristicSchema;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Dictionaries.AddTerm;

public sealed class AddCatalogDictionaryTermCommandHandler
{
    private readonly ICatalogDictionaryRepository _dictionaryRepository;
    private readonly ICatalogProductTypeSchemaReader _productTypeSchemaReader;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public AddCatalogDictionaryTermCommandHandler(
        ICatalogDictionaryRepository dictionaryRepository,
        ICatalogProductTypeSchemaReader productTypeSchemaReader,
        IUserRepository userRepository,
        ICurrentUserProvider currentUserProvider)
    {
        ArgumentNullException.ThrowIfNull(dictionaryRepository);
        ArgumentNullException.ThrowIfNull(productTypeSchemaReader);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(currentUserProvider);

        _dictionaryRepository = dictionaryRepository;
        _productTypeSchemaReader = productTypeSchemaReader;
        _userRepository = userRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<AddCatalogDictionaryTermResult, DomainError>> Handle(
        AddCatalogDictionaryTermCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currentUserId = _currentUserProvider.UserId;

        if (!currentUserId.HasValue)
        {
            return CatalogErrors.CurrentUserIsRequired();
        }

        var user = await _userRepository
            .GetByIdAsync(currentUserId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !user.CanManageProductSynonyms())
        {
            return CatalogErrors.OnlyTechnicalUserCanManageDictionaryTerms();
        }

        if (!Enum.TryParse<CatalogDictionaryTermKind>(
                command.Kind,
                ignoreCase: true,
                out var kind) ||
            !Enum.IsDefined(kind) ||
            kind == CatalogDictionaryTermKind.None)
        {
            return GeneralErrors.ValueIsInvalid(nameof(command.Kind));
        }

        CatalogProductTypeCharacteristicSchemaResult? productTypeSchema = null;

        if (!string.IsNullOrWhiteSpace(command.ProductTypeCode))
        {
            productTypeSchema = await _productTypeSchemaReader
                .GetByCodeAsync(
                    command.ProductTypeCode.Trim(),
                    cancellationToken)
                .ConfigureAwait(false);

            if (productTypeSchema is null)
            {
                return CatalogErrors.ProductTypeNotFound(
                    command.ProductTypeCode);
            }
        }

        if (kind == CatalogDictionaryTermKind.Characteristic &&
            productTypeSchema is not null)
        {
            var normalizedTargetCode = NormalizeCode(
                command.TargetCode);

            var targetCharacteristicIsAllowed =
                productTypeSchema.Characteristics.Any(
                    characteristic => string.Equals(
                        NormalizeCode(characteristic.Code),
                        normalizedTargetCode,
                        StringComparison.Ordinal));

            if (!targetCharacteristicIsAllowed)
            {
                return CatalogErrors
                    .DictionaryTargetCharacteristicIsNotAllowedForProductType(
                        command.TargetCode ?? string.Empty,
                        productTypeSchema.ProductTypeCode);
            }
        }

        var productTypeId = productTypeSchema?.ProductTypeId;

        var termResult = CatalogDictionaryTerm.Create(
            command.Phrase,
            kind,
            command.TargetCode,
            command.TargetValue,
            command.Priority,
            CatalogDictionaryTermStatus.Approved,
            CatalogDictionaryTermSource.Admin,
            productTypeId);

        if (termResult.IsFailure)
        {
            return termResult.Error;
        }

        var term = termResult.Value;

        var alreadyExists = await _dictionaryRepository
            .ExistsAsync(term, cancellationToken)
            .ConfigureAwait(false);

        if (alreadyExists)
        {
            return CatalogErrors.DictionaryTermAlreadyExists(
                command.Phrase);
        }

        _dictionaryRepository.Add(term);

        await _dictionaryRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return new AddCatalogDictionaryTermResult(
            term.Id,
            term.ProductTypeId,
            productTypeSchema?.ProductTypeCode,
            term.Phrase,
            term.NormalizedPhrase,
            term.Kind.ToString(),
            term.TargetCode,
            term.TargetValue,
            term.Status.ToString(),
            term.Source.ToString(),
            term.Priority);
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