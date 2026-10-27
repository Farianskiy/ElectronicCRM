using ElectronicService.Core.Catalog.Products.Analogies;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogProductAnalogyConfigurationRepository(
    ElectronicDbContext dbContext)
    : ICatalogProductAnalogyConfigurationRepository
{
    public Task<CatalogProductAnalogyConfiguration?> GetActiveAsync(
        Guid manufacturerId,
        Guid productTypeId,
        CancellationToken cancellationToken = default) =>
        dbContext.CatalogProductAnalogyConfigurations
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.ManufacturerId == manufacturerId
                && x.ProductTypeId == productTypeId
                && x.Status == CatalogProductAnalogyConfigurationStatus.Active,
                cancellationToken);

    public Task<CatalogProductAnalogyConfiguration?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.CatalogProductAnalogyConfigurations
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<CatalogProductAnalogyConfiguration>> ListAsync(
        Guid manufacturerId,
        Guid productTypeId,
        CancellationToken cancellationToken = default) =>
        await dbContext.CatalogProductAnalogyConfigurations
            .AsNoTracking()
            .Where(x => x.ManufacturerId == manufacturerId
                && x.ProductTypeId == productTypeId)
            .OrderByDescending(x => x.VersionNumber)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<int> GetNextVersionNumberAsync(
        Guid manufacturerId,
        Guid productTypeId,
        CancellationToken cancellationToken = default)
    {
        var latest = await dbContext.CatalogProductAnalogyConfigurations
            .Where(x => x.ManufacturerId == manufacturerId
                && x.ProductTypeId == productTypeId)
            .MaxAsync(x => (int?)x.VersionNumber, cancellationToken)
            .ConfigureAwait(false);
        return (latest ?? 0) + 1;
    }

    public void Add(CatalogProductAnalogyConfiguration configuration) =>
        dbContext.CatalogProductAnalogyConfigurations.Add(configuration);

    public void Add(CatalogProductAnalogyEvaluationReport report) =>
        dbContext.CatalogProductAnalogyEvaluationReports.Add(report);

    public async Task<IReadOnlyCollection<CatalogProductAnalogyEvaluationReport>> ListReportsAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default) =>
        await dbContext.CatalogProductAnalogyEvaluationReports.AsNoTracking()
            .Where(x => x.ConfigurationId == configurationId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);

    public Task<CatalogProductAnalogyEvaluationReport?> GetReportAsync(
        Guid reportId,
        CancellationToken cancellationToken = default) =>
        dbContext.CatalogProductAnalogyEvaluationReports.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == reportId, cancellationToken);

    public async Task<bool> TryActivateAsync(
        CatalogProductAnalogyConfiguration configuration,
        uint expectedVersion,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (configuration.Version != expectedVersion)
            {
                return false;
            }

            var active = await dbContext.CatalogProductAnalogyConfigurations
                .SingleOrDefaultAsync(x => x.ManufacturerId == configuration.ManufacturerId
                    && x.ProductTypeId == configuration.ProductTypeId
                    && x.Status == CatalogProductAnalogyConfigurationStatus.Active,
                    cancellationToken).ConfigureAwait(false);
            if (active is not null)
            {
                var superseded = active.Supersede();
                if (superseded.IsFailure) return false;
            }

            var activated = configuration.Activate();
            if (activated.IsFailure) return false;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }
    }

    public async Task<bool> TrySaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
}
