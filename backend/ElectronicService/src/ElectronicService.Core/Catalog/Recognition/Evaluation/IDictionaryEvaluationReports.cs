using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Evaluation;

public interface IRecognitionReleaseTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}
public interface IRecognitionReleaseSession
{
    Task<IRecognitionReleaseTransaction> BeginAsync(CancellationToken ct);
    Task<Result<Guid, DomainError>> AuthorizeAsync(CancellationToken ct);
}
public interface IDictionaryEvaluationReports
{
    Task<Result<Guid, DomainError>> CreateAsync(ApproveCatalogAssistantDictionarySuggestionCommand command, CancellationToken ct);
    Task<Result<DictionaryEvaluationPage, DomainError>> ReadAsync(Guid id, int page, bool replay, CancellationToken ct);
    Task<UnitResult<DomainError>> ValidateAsync(ApproveCatalogAssistantDictionarySuggestionCommand command, CatalogDictionarySuggestionApprovalDecision decision, CancellationToken ct);
}
public sealed record DictionaryEvaluationDecision(string Phrase, CatalogDictionaryTermKind Kind, string? TargetCode, string TargetValue,
    Guid? ManufacturerId, Guid? ProductTypeId, Guid? CharacteristicDefinitionId, int Priority)
{
    public static DictionaryEvaluationDecision From(CatalogDictionarySuggestionApprovalDecision value) =>
        new(value.Phrase, value.Kind, value.TargetCode, value.TargetValue, value.ManufacturerId, value.ProductTypeId, value.CharacteristicDefinitionId, value.Priority);
    public Result<CatalogDictionaryTerm, DomainError> CreateTerm() => CatalogDictionaryTerm.Create(Phrase, Kind, TargetCode, TargetValue, Priority,
        CatalogDictionaryTermStatus.Approved, CatalogDictionaryTermSource.UserCorrection, ManufacturerId, ProductTypeId);
}
public sealed record DictionaryEvaluationSnapshot(int FormatVersion, string EvaluatorVersion, Guid SuggestionId, Guid CandidateId,
    long EvidenceRevision, DictionaryEvaluationDecision Decision, string? ReviewComment, string ProductTypeCode,
    EvaluationInput Input, string InputFingerprint, string EvidenceFingerprint, EvaluationResult Result, bool SufficientEvidence);
public sealed record DictionaryEvaluationPage(EvaluationPage Evaluation, Guid SuggestionId, DictionaryEvaluationDecision Decision, bool UsedForApproval);
