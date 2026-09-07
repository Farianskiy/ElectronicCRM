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
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<CatalogDictionaryTermResult>> GetTermsAsync(CancellationToken cancellationToken = default)
    {
        var terms = await GetBaseQuery()
            .Select(term => new CatalogDictionaryTermData(
                term.Id,
                term.ManufacturerId,
                term.ProductTypeId,
                term.Phrase,
                term.NormalizedPhrase,
                term.Kind,
                term.TargetCode,
                term.TargetValue,
                term.Priority,
                term.Status,
                term.Source,
                term.CreatedAtUtc,
                term.ApprovedAtUtc,
                term.DisabledAtUtc,
                term.DisabledByUserId,
                term.DisableReason,
                term.ReactivatedAtUtc,
                term.ReactivatedByUserId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var userDisplayNames = await GetLifecycleUserDisplayNamesAsync(terms, cancellationToken).ConfigureAwait(false);

        return terms.Select(term => MapToResult(term, userDisplayNames)).ToList();
    }

    public async Task<IReadOnlyCollection<CatalogDictionaryTermResult>> GetApprovedTermsAsync(CancellationToken cancellationToken = default)
    {
        var terms = await GetBaseQuery()
            .Where(term => term.Status == CatalogDictionaryTermStatus.Approved)
            .Select(term => new CatalogDictionaryTermData(
                term.Id,
                term.ManufacturerId,
                term.ProductTypeId,
                term.Phrase,
                term.NormalizedPhrase,
                term.Kind,
                term.TargetCode,
                term.TargetValue,
                term.Priority,
                term.Status,
                term.Source,
                term.CreatedAtUtc,
                term.ApprovedAtUtc,
                term.DisabledAtUtc,
                term.DisabledByUserId,
                term.DisableReason,
                term.ReactivatedAtUtc,
                term.ReactivatedByUserId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return terms.Select(term => MapToResult(term, null)).ToList();
    }

    private IQueryable<CatalogDictionaryTerm> GetBaseQuery()
    {
        return _dbContext.CatalogDictionaryTerms
            .AsNoTracking()
            .OrderByDescending(term => term.Priority)
            .ThenByDescending(term => term.NormalizedPhrase.Length);
    }

    private async Task<Dictionary<Guid, string>> GetLifecycleUserDisplayNamesAsync(
        IReadOnlyCollection<CatalogDictionaryTermData> terms,
        CancellationToken cancellationToken)
    {
        var userIds = terms
            .SelectMany(term => new[]
            {
                term.DisabledByUserId,
                term.ReactivatedByUserId
            })
            .Where(userId => userId.HasValue)
            .Select(userId => userId!.Value)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return users.ToDictionary(user => user.Id, user => user.DisplayName.Value);
    }

    private static CatalogDictionaryTermResult MapToResult(CatalogDictionaryTermData term, Dictionary<Guid, string>? userDisplayNames)
    {
        return new CatalogDictionaryTermResult(
            term.Id,
            term.ManufacturerId,
            term.ProductTypeId,
            term.Phrase,
            term.NormalizedPhrase,
            term.Kind.ToString(),
            term.TargetCode,
            term.TargetValue,
            term.Priority,
            term.Status.ToString(),
            term.Source.ToString(),
            term.CreatedAtUtc,
            term.ApprovedAtUtc,
            term.DisabledAtUtc,
            term.DisabledByUserId,
            GetUserDisplayName(term.DisabledByUserId, userDisplayNames),
            term.DisableReason,
            term.ReactivatedAtUtc,
            term.ReactivatedByUserId,
            GetUserDisplayName(term.ReactivatedByUserId, userDisplayNames));
    }

    private static string? GetUserDisplayName(Guid? userId, Dictionary<Guid, string>? userDisplayNames)
    {
        if (!userId.HasValue || userDisplayNames is null)
        {
            return null;
        }

        return userDisplayNames.GetValueOrDefault(userId.Value);
    }

    private sealed record CatalogDictionaryTermData(
        Guid Id,
        Guid? ManufacturerId,
        Guid? ProductTypeId,
        string Phrase,
        string NormalizedPhrase,
        CatalogDictionaryTermKind Kind,
        string? TargetCode,
        string TargetValue,
        int Priority,
        CatalogDictionaryTermStatus Status,
        CatalogDictionaryTermSource Source,
        DateTime CreatedAtUtc,
        DateTime? ApprovedAtUtc,
        DateTime? DisabledAtUtc,
        Guid? DisabledByUserId,
        string? DisableReason,
        DateTime? ReactivatedAtUtc,
        Guid? ReactivatedByUserId);
}