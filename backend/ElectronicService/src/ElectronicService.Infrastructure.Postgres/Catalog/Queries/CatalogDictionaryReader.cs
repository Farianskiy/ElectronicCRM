using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Queries;

public sealed class CatalogDictionaryReader : ICatalogDictionaryReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogDictionaryReader(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<CatalogDictionaryTermResult>> GetTermsAsync(
        CancellationToken cancellationToken = default)
    {
        var terms = await GetBaseQuery()
            .Select(term => new CatalogDictionaryTermData(
                term.Id,
                term.Phrase,
                term.NormalizedPhrase,
                term.Kind,
                term.TargetCode,
                term.TargetValue,
                term.Priority,
                term.Status,
                term.Source))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return terms
            .Select(MapToResult)
            .ToList();
    }

    public async Task<IReadOnlyCollection<CatalogDictionaryTermResult>> GetApprovedTermsAsync(
        CancellationToken cancellationToken = default)
    {
        var terms = await GetBaseQuery()
            .Where(term => term.Status == CatalogDictionaryTermStatus.Approved)
            .Select(term => new CatalogDictionaryTermData(
                term.Id,
                term.Phrase,
                term.NormalizedPhrase,
                term.Kind,
                term.TargetCode,
                term.TargetValue,
                term.Priority,
                term.Status,
                term.Source))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return terms
            .Select(MapToResult)
            .ToList();
    }

    private IQueryable<CatalogDictionaryTerm> GetBaseQuery()
    {
        return _dbContext.CatalogDictionaryTerms
            .AsNoTracking()
            .OrderByDescending(term => term.Priority)
            .ThenByDescending(term => term.NormalizedPhrase.Length);
    }

    private static CatalogDictionaryTermResult MapToResult(
        CatalogDictionaryTermData term)
    {
        return new CatalogDictionaryTermResult(
            term.Id,
            term.Phrase,
            term.NormalizedPhrase,
            term.Kind.ToString(),
            term.TargetCode,
            term.TargetValue,
            term.Priority,
            term.Status.ToString(),
            term.Source.ToString());
    }

    private sealed record CatalogDictionaryTermData(
        Guid Id,
        string Phrase,
        string NormalizedPhrase,
        CatalogDictionaryTermKind Kind,
        string? TargetCode,
        string TargetValue,
        int Priority,
        CatalogDictionaryTermStatus Status,
        CatalogDictionaryTermSource Source);
}