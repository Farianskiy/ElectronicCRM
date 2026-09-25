using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Dictionaries;

public sealed class CatalogDictionaryEvaluationReport : ElectronicService.Domain.Abstractions.Entity
{
    private CatalogDictionaryEvaluationReport() { }
    private CatalogDictionaryEvaluationReport(Guid suggestionId, Guid candidateId, Guid author, string snapshot) : base(Guid.CreateVersion7())
    { SuggestionId = suggestionId; CandidateId = candidateId; CreatedByUserId = author; SnapshotJson = snapshot; CreatedAtUtc = DateTime.UtcNow; }
    public Guid SuggestionId { get; private set; }
    public Guid CandidateId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string SnapshotJson { get; private set; } = "{}";
    public static Result<CatalogDictionaryEvaluationReport, DomainError> Create(Guid suggestionId, Guid candidateId, Guid author, string snapshot)
    {
        if (suggestionId == Guid.Empty || candidateId == Guid.Empty || author == Guid.Empty)
            return new DomainError("evaluation.invalid_snapshot", "Не указаны предложение, кандидат или автор.");
        if (Encoding.UTF8.GetByteCount(snapshot) > 8_000_000)
            return new DomainError("evaluation.selection_too_large", "Снимок превышает допустимый размер; отчёт не сохранён.");
        using var json = JsonDocument.Parse(snapshot);
        if (json.RootElement.ValueKind != JsonValueKind.Object) return new DomainError("evaluation.invalid_snapshot", "Некорректный снимок.");
        return new CatalogDictionaryEvaluationReport(suggestionId, candidateId, author, snapshot);
    }
}
