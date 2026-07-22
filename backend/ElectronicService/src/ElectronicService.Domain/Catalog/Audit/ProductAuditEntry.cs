using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Audit;

public sealed class ProductAuditEntry : AggregateRoot
{
    private ProductAuditEntry(
        Guid id,
        Guid productId,
        Guid? changedByUserId,
        ProductAuditOperation operation,
        ProductAuditSource source,
        Guid? sourceId,
        DateTime changedAtUtc,
        string? beforeJson,
        string? afterJson)
        : base(id)
    {
        ProductId = productId;
        ChangedByUserId = changedByUserId;
        Operation = operation;
        Source = source;
        SourceId = sourceId;
        ChangedAtUtc = changedAtUtc;
        BeforeJson = beforeJson;
        AfterJson = afterJson;
    }

    private ProductAuditEntry()
    {
    }

    public Guid ProductId { get; private set; }

    public Guid? ChangedByUserId { get; private set; }

    public ProductAuditOperation Operation { get; private set; }

    public ProductAuditSource Source { get; private set; }

    /// <summary>
    /// Идентификатор источника изменения.
    ///
    /// Для ImportBatch здесь будет CatalogImportBatchId.
    /// Для ручного изменения обычно остаётся null.
    /// </summary>
    public Guid? SourceId { get; private set; }

    public DateTime ChangedAtUtc { get; private set; }

    public string? BeforeJson { get; private set; }

    public string? AfterJson { get; private set; }

    public static Result<ProductAuditEntry, DomainError> Create(
        Guid productId,
        Guid? changedByUserId,
        ProductAuditOperation operation,
        ProductAuditSource source,
        Guid? sourceId,
        string? beforeJson,
        string? afterJson)
    {
        if (productId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(productId));
        }

        if (operation == ProductAuditOperation.None)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(operation));
        }

        if (source == ProductAuditSource.None)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(source));
        }

        if (changedByUserId.HasValue
            && changedByUserId.Value == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(changedByUserId));
        }

        if (sourceId.HasValue
            && sourceId.Value == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(sourceId));
        }

        /*
         * Ручное изменение должно иметь
         * конкретного пользователя.
         */
        if (source == ProductAuditSource.Manual
            && !changedByUserId.HasValue)
        {
            return GeneralErrors.ValueIsRequired(
                nameof(changedByUserId));
        }

        /*
         * Изменение из staging должно быть связано
         * с конкретным import batch.
         */
        if (source == ProductAuditSource.ImportBatch
            && !sourceId.HasValue)
        {
            return GeneralErrors.ValueIsRequired(
                nameof(sourceId));
        }

        if (string.IsNullOrWhiteSpace(beforeJson)
            && string.IsNullOrWhiteSpace(afterJson))
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(afterJson));
        }

        /*
        * Защита последнего уровня:
        * одинаковые JSON не являются событием аудита.
        */
        if (!string.IsNullOrWhiteSpace(beforeJson)
            && string.Equals(
                beforeJson,
                afterJson,
                StringComparison.Ordinal))
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(afterJson));
        }

        return new ProductAuditEntry(
            Guid.CreateVersion7(),
            productId,
            changedByUserId,
            operation,
            source,
            sourceId,
            DateTime.UtcNow,
            beforeJson,
            afterJson);
    }
}