using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Dictionaries.AddTerm;

public sealed class AddCatalogDictionaryTermCommandHandler
{
    private readonly ICatalogDictionaryRepository _dictionaryRepository;

    public AddCatalogDictionaryTermCommandHandler(
        ICatalogDictionaryRepository dictionaryRepository)
    {
        _dictionaryRepository = dictionaryRepository;
    }

    public async Task<Result<AddCatalogDictionaryTermResult, DomainError>> Handle(
        AddCatalogDictionaryTermCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!Enum.TryParse<CatalogDictionaryTermKind>(
                command.Kind,
                ignoreCase: true,
                out var kind))
        {
            return GeneralErrors.ValueIsInvalid(nameof(command.Kind));
        }

        var termResult = CatalogDictionaryTerm.Create(
            command.Phrase,
            kind,
            command.TargetCode,
            command.TargetValue,
            command.Priority,
            CatalogDictionaryTermStatus.Approved,
            CatalogDictionaryTermSource.UserCorrection);

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
            return CatalogErrors.DictionaryTermAlreadyExists(command.Phrase);
        }

        _dictionaryRepository.Add(term);

        await _dictionaryRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return new AddCatalogDictionaryTermResult(
            term.Id,
            term.Phrase,
            term.Kind.ToString(),
            term.TargetCode,
            term.TargetValue,
            term.Status.ToString(),
            term.Source.ToString(),
            term.Priority);
    }
}