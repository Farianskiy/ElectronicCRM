using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogDictionaryRepository : ICatalogDictionaryRepository
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogDictionaryRepository(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<CatalogDictionaryTerm?> GetByIdAsync(Guid termId, CancellationToken cancellationToken = default)
    {
        return _dbContext.CatalogDictionaryTerms.SingleOrDefaultAsync(term => term.Id == termId, cancellationToken);
    }

    public Task<bool> ExistsAsync(CatalogDictionaryTerm term, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(term);

        return _dbContext.CatalogDictionaryTerms
            .AsNoTracking()
            .AnyAsync(
                existingTerm =>
                    existingTerm.ManufacturerId == term.ManufacturerId &&
                    existingTerm.ProductTypeId == term.ProductTypeId &&
                    existingTerm.NormalizedPhrase == term.NormalizedPhrase &&
                    existingTerm.Kind == term.Kind &&
                    existingTerm.TargetCode == term.TargetCode &&
                    existingTerm.TargetValue == term.TargetValue,
                cancellationToken);
    }

    public void Add(CatalogDictionaryTerm term)
    {
        ArgumentNullException.ThrowIfNull(term);

        _dbContext.CatalogDictionaryTerms.Add(term);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}