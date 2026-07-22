using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Infrastructure.Postgres.Data;

namespace ElectronicService.Infrastructure.Postgres
    .Catalog.Repositories;

public sealed class ProductAuditRepository
    : IProductAuditRepository
{
    private readonly ElectronicDbContext _dbContext;

    public ProductAuditRepository(
        ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(
        ProductAuditEntry auditEntry)
    {
        ArgumentNullException.ThrowIfNull(
            auditEntry);

        _dbContext.ProductAuditEntries.Add(
            auditEntry);
    }
}