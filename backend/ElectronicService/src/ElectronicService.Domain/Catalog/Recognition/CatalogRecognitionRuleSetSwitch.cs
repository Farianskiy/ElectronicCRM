using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Recognition;

public sealed class CatalogRecognitionRuleSetSwitch : AggregateRoot
{
    private CatalogRecognitionRuleSetSwitch()
    {
    }

    private CatalogRecognitionRuleSetSwitch(Guid id) : base(id)
    {
    }

    public Guid ManufacturerId { get; private set; }
    public Guid ProductTypeId { get; private set; }
    public long SequenceNumber { get; private set; }

    public Guid? PreviousVersionId { get; private set; }
    public Guid? NewVersionId { get; private set; }
    public Guid? ReportId { get; private set; }

    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    public static Result<CatalogRecognitionRuleSetSwitch, DomainError> Create(
        CatalogRecognitionRuleSetSwitchData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.ManufacturerId == Guid.Empty ||
            data.ProductTypeId == Guid.Empty ||
            data.CreatedByUserId == Guid.Empty)
        {
            return Invalid("Укажите производителя, тип товара и автора.");
        }

        if (data.SequenceNumber < 1)
        {
            return Invalid("Номер переключения должен быть положительным.");
        }

        if (data.PreviousVersionId == Guid.Empty ||
            data.NewVersionId == Guid.Empty ||
            data.ReportId == Guid.Empty)
        {
            return Invalid(
                "Необязательные идентификаторы должны содержать значение или null.");
        }

        if (data.PreviousVersionId == data.NewVersionId)
        {
            return Invalid("Переключение не изменяет активную версию.");
        }

        if (data.SequenceNumber == 1 && data.PreviousVersionId.HasValue)
        {
            return Invalid(
                "Первое переключение не может иметь предыдущую активную версию.");
        }

        if (data.NewVersionId.HasValue != data.ReportId.HasValue)
        {
            return Invalid(
                "Для включения версии требуется отчёт. При отключении отчёт не указывается.");
        }

        if (string.IsNullOrWhiteSpace(data.Reason) ||
            data.Reason.Length > 1000)
        {
            return Invalid(
                "Укажите причину переключения длиной от 1 до 1000 символов.");
        }

        return new CatalogRecognitionRuleSetSwitch(Guid.CreateVersion7())
        {
            ManufacturerId = data.ManufacturerId,
            ProductTypeId = data.ProductTypeId,
            SequenceNumber = data.SequenceNumber,
            PreviousVersionId = data.PreviousVersionId,
            NewVersionId = data.NewVersionId,
            ReportId = data.ReportId,
            CreatedByUserId = data.CreatedByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            Reason = data.Reason.Trim()
        };
    }

    private static Result<CatalogRecognitionRuleSetSwitch, DomainError> Invalid(
        string message)
    {
        return new DomainError("training.invalid_switch", message);
    }
}