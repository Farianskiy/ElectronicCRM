using ElectronicService.Domain.Catalog.Audit;

namespace ElectronicService.Core.Catalog.Products.Audit;

public interface IProductAuditRepository
{
    void Add(ProductAuditEntry auditEntry);
}