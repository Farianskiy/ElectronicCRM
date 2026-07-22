using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.Audit;

public sealed class ProductAuditRecorder
{
    private readonly IProductAuditRepository
        _auditRepository;

    public ProductAuditRecorder(
        IProductAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public UnitResult<DomainError> RecordManualChange(
        Product product,
        Guid changedByUserId,
        ProductAuditOperation operation,
        string beforeJson)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (changedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(
                CatalogErrors.CurrentUserIsRequired());
        }

        if (string.IsNullOrWhiteSpace(beforeJson))
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(beforeJson)));
        }

        var afterJson =
            ProductAuditSnapshotSerializer.Serialize(
                product);

        var auditEntryResult =
            ProductAuditEntry.Create(
                product.Id,
                changedByUserId,
                operation,
                ProductAuditSource.Manual,
                sourceId: null,
                beforeJson,
                afterJson);

        if (auditEntryResult.IsFailure)
        {
            return UnitResult.Failure(
                auditEntryResult.Error);
        }

        _auditRepository.Add(
            auditEntryResult.Value);

        return UnitResult.Success<DomainError>();
    }
}