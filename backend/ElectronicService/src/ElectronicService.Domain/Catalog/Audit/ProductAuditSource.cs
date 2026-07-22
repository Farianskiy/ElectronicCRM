namespace ElectronicService.Domain.Catalog.Audit;

public enum ProductAuditSource
{
    None = 0,

    Manual = 1,
    ImportBatch = 2,
    System = 3
}