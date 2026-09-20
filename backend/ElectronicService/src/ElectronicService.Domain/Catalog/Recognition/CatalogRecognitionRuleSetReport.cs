using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Recognition;

public sealed class CatalogRecognitionRuleSetReport : AggregateRoot
{
    private const int MaxSnapshotLength = 16_000_000;

    private CatalogRecognitionRuleSetReport()
    {
    }

    private CatalogRecognitionRuleSetReport(Guid id) : base(id)
    {
    }

    public Guid RuleSetVersionId { get; private set; }
    public Guid BatchId { get; private set; }
    public long BatchVersion { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    public DateTime StartedAtUtc { get; private set; }
    public DateTime CompletedAtUtc { get; private set; }

    public string EvaluatorVersion { get; private set; } = string.Empty;
    public int SnapshotFormatVersion { get; private set; }

    public int TotalRowsCount { get; private set; }
    public int ProposedRowsCount { get; private set; }
    public int ConflictRowsCount { get; private set; }
    public int NoMatchRowsCount { get; private set; }
    public int OutsideScopeRowsCount { get; private set; }

    public string SnapshotJson { get; private set; } = string.Empty;

    public static Result<CatalogRecognitionRuleSetReport, DomainError> Create(
        CatalogRecognitionRuleSetReportData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.RuleSetVersionId == Guid.Empty ||
            data.BatchId == Guid.Empty ||
            data.CreatedByUserId == Guid.Empty)
        {
            return new DomainError(
                "training.invalid_report",
                "Укажите версию правил, пакет и автора отчёта.");
        }

        if (data.StartedAtUtc.Kind != DateTimeKind.Utc ||
            data.CompletedAtUtc.Kind != DateTimeKind.Utc ||
            data.StartedAtUtc == DateTime.MinValue ||
            data.CompletedAtUtc < data.StartedAtUtc)
        {
            return new DomainError(
                "training.invalid_report",
                "Некорректное время проверки.");
        }

        if (string.IsNullOrWhiteSpace(data.EvaluatorVersion) ||
            data.EvaluatorVersion.Length > 100)
        {
            return new DomainError(
                "training.invalid_report",
                "Укажите версию исполнителя длиной до 100 символов.");
        }

        if (data.TotalRowsCount < 1 ||
            data.ProposedRowsCount < 0 ||
            data.ConflictRowsCount < 0 ||
            data.NoMatchRowsCount < 0 ||
            data.OutsideScopeRowsCount < 0)
        {
            return new DomainError(
                "training.invalid_report",
                "Отчёт должен содержать строки и неотрицательные счётчики.");
        }

        var classifiedRows =
            (long)data.ProposedRowsCount +
            data.ConflictRowsCount +
            data.NoMatchRowsCount +
            data.OutsideScopeRowsCount;

        if (classifiedRows != data.TotalRowsCount)
        {
            return new DomainError(
                "training.invalid_report",
                "Сумма результатов не совпадает с количеством проверенных строк.");
        }

        if (!IsValidSnapshot(data.SnapshotJson, data.TotalRowsCount))
        {
            return new DomainError(
                "training.invalid_report",
                "Снимок отчёта повреждён, превышает размер или содержит неполный список строк.");
        }

        return new CatalogRecognitionRuleSetReport(Guid.CreateVersion7())
        {
            RuleSetVersionId = data.RuleSetVersionId,
            BatchId = data.BatchId,
            BatchVersion = data.BatchVersion,
            CreatedByUserId = data.CreatedByUserId,
            StartedAtUtc = data.StartedAtUtc,
            CompletedAtUtc = data.CompletedAtUtc,
            EvaluatorVersion = data.EvaluatorVersion.Trim(),
            SnapshotFormatVersion = 1,
            TotalRowsCount = data.TotalRowsCount,
            ProposedRowsCount = data.ProposedRowsCount,
            ConflictRowsCount = data.ConflictRowsCount,
            NoMatchRowsCount = data.NoMatchRowsCount,
            OutsideScopeRowsCount = data.OutsideScopeRowsCount,
            SnapshotJson = data.SnapshotJson
        };
    }

    private static bool IsValidSnapshot(string snapshotJson, int expectedRows)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson) ||
            snapshotJson.Length > MaxSnapshotLength)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(snapshotJson);

            return document.RootElement.ValueKind == JsonValueKind.Array &&
                document.RootElement.GetArrayLength() == expectedRows;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}