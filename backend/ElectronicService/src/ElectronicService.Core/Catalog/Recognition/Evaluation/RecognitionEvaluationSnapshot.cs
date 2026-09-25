using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Catalog.Characteristics;

namespace ElectronicService.Core.Catalog.Recognition.Evaluation;

public sealed class RecognitionEvaluationOptions
{
    public const string SectionName = "RecognitionEvaluation";
    public int MinimumControlNames { get; set; } = 1;
    public int MinimumControlUnits { get; set; } = 1;
    public int MinimumImprovements { get; set; } = 1;
    public int MaxExamples { get; set; } = 2000;
    public int TimeoutSeconds { get; set; } = 60;
    public bool IsValid() => MinimumControlNames is >= 1 and <= 2000 && MinimumControlUnits is >= 1 and <= 2000 &&
        MinimumImprovements is >= 1 and <= 2000 && MaxExamples is >= 1 and <= 2000 && TimeoutSeconds is >= 1 and <= 120 &&
        MinimumControlNames <= MaxExamples && MinimumControlUnits <= MaxExamples && MinimumImprovements <= MaxExamples;
    public EvaluationPolicy Policy() => new("release-policy-v1", MinimumControlNames, MinimumControlUnits, MinimumImprovements);
}

public sealed record EvaluationPolicy(string Version, int MinimumControlNames, int MinimumControlUnits, int MinimumImprovements);
public sealed record EvaluationDefinition(Guid Id, string Code, string Name, CharacteristicDataType DataType, string? Unit);
public sealed record EvaluationExample(Guid Id, Guid FeedbackId, string ProductName, Guid CharacteristicId, string Value, bool EvaluationOnly);
public sealed record EvaluationInput(Guid ManufacturerId, string ManufacturerName, Guid ProductTypeId,
    CatalogRecognitionRuleSetState CurrentState, CatalogRecognitionRuleSetExecutionSnapshot? CurrentRules,
    CatalogRecognitionRuleSetExecutionSnapshot? CandidateRules, IReadOnlyList<EvaluationDefinition> Definitions,
    IReadOnlyList<CatalogDictionaryTermResult> Terms, IReadOnlyList<CatalogCharacteristicRecognitionProfileResult> Profiles,
    IReadOnlyList<EvaluationExample> Examples, IReadOnlyList<string> EvidenceNames, EvaluationPolicy Policy)
{
    public string SchemaJson { get; init; } = "[]";
    public string ProductTypeName { get; init; } = "";
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<CatalogDictionaryTermResult>? CandidateTerms { get; init; }
}
public sealed record EvaluationSource(Guid ExampleId, Guid FeedbackId);
public sealed record EvaluationOutcome(string Status, string? Value);
public sealed record EvaluationRow(string ProductName, string NormalizedName, Guid CharacteristicId, string CharacteristicCode,
    IReadOnlyList<string> ExpectedValues, IReadOnlyList<EvaluationSource> Sources, bool TrainingOverlap, bool LabelConflict,
    EvaluationOutcome Current, EvaluationOutcome Candidate, string Change);
public sealed record EvaluationCounts(int Total, int Correct, int Incorrect, int Missing, int Conflicts)
{
    public decimal? CorrectRate => Rate(Correct);
    public decimal? IncorrectRate => Rate(Incorrect);
    public decimal? MissingRate => Rate(Missing);
    public decimal? ConflictRate => Rate(Conflicts);
    private decimal? Rate(int count) => Total == 0 ? null : (decimal)count / Total;
}
public sealed record EvaluationMetrics(int Names, EvaluationCounts Current, EvaluationCounts Candidate, int Improvements, int Regressions);
public sealed record EvaluationCharacteristicMetrics(Guid CharacteristicId, EvaluationMetrics Metrics);
public sealed record EvaluationReason(string Code, string Message);
public sealed record EvaluationReadiness(string State, IReadOnlyList<EvaluationReason> Reasons);
public sealed record EvaluationResult(int InputExamples, int DuplicateExamples, int LabelConflicts, int OverlapUnits,
    EvaluationMetrics Control, EvaluationMetrics TrainingDiagnostic, IReadOnlyList<EvaluationCharacteristicMetrics> Characteristics,
    IReadOnlyList<EvaluationRow> Rows, EvaluationReadiness Readiness);
public sealed record RecognitionEvaluationSnapshot(int FormatVersion, string EvaluatorVersion, EvaluationInput Input,
    string InputFingerprint, EvaluationResult Result, CatalogRecognitionRuleSetTrainingCheckResult Training);
public sealed record EvaluationPage(Guid ReportId, Guid ManufacturerId, Guid ProductTypeId, Guid? CurrentVersionId,
    Guid CandidateVersionId, long SequenceNumber, bool HistoricalReplay, EvaluationPolicy? Policy,
    EvaluationReadiness Readiness, EvaluationMetrics? Control, EvaluationMetrics? TrainingDiagnostic,
    IReadOnlyList<EvaluationCharacteristicMetrics> Characteristics, int InputExamples, int DuplicateExamples,
    int LabelConflicts, int OverlapUnits, bool? TrainingPassed, int Page, int Total, IReadOnlyList<EvaluationRow> Items)
{
    public string? ManufacturerName { get; init; }
    public string? ProductTypeName { get; init; }
}
