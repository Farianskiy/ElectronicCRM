using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Products;

public sealed class CatalogProductAnalogyEvaluationReport : AggregateRoot
{
    public const int HashMaxLength = 64;
    public const int EvaluatorVersionMaxLength = 100;
    public const int SnapshotJsonMaxLength = 16_000_000;

    private CatalogProductAnalogyEvaluationReport() { }
    private CatalogProductAnalogyEvaluationReport(Guid id) : base(id) { }

    public Guid ConfigurationId { get; private set; }
    public string ConfigurationHash { get; private set; } = string.Empty;
    public Guid? BaselineConfigurationId { get; private set; }
    public string BaselineConfigurationHash { get; private set; } = string.Empty;
    public string CandidateSnapshotId { get; private set; } = string.Empty;
    public string EvaluatorVersion { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public int ControlProducts { get; private set; }
    public int ControlValues { get; private set; }
    public int Improvements { get; private set; }
    public int Regressions { get; private set; }
    public int CandidateCorrect { get; private set; }
    public int CandidateIncorrect { get; private set; }
    public bool IsReady { get; private set; }
    public string SnapshotJson { get; private set; } = string.Empty;

    public static Result<CatalogProductAnalogyEvaluationReport, DomainError> Create(
        Guid configurationId, string configurationHash,
        Guid? baselineConfigurationId, string baselineConfigurationHash,
        string candidateSnapshotId, string evaluatorVersion, Guid userId,
        int controlProducts, int controlValues, int improvements, int regressions,
        int candidateCorrect, int candidateIncorrect, bool isReady, string snapshotJson)
    {
        if (configurationId == Guid.Empty || userId == Guid.Empty
            || string.IsNullOrWhiteSpace(configurationHash)
            || string.IsNullOrWhiteSpace(baselineConfigurationHash)
            || string.IsNullOrWhiteSpace(candidateSnapshotId)
            || string.IsNullOrWhiteSpace(evaluatorVersion)
            || configurationHash.Length > HashMaxLength
            || baselineConfigurationHash.Length > HashMaxLength
            || evaluatorVersion.Length > EvaluatorVersionMaxLength
            || controlProducts < 0 || controlValues < 0 || improvements < 0
            || regressions < 0 || candidateCorrect < 0 || candidateIncorrect < 0
            || string.IsNullOrWhiteSpace(snapshotJson)
            || snapshotJson.Length > SnapshotJsonMaxLength)
        {
            return new DomainError("catalog_analogy.invalid_report", "Отчёт оценки аналогов содержит недопустимые данные.");
        }

        return new CatalogProductAnalogyEvaluationReport(Guid.CreateVersion7())
        {
            ConfigurationId = configurationId,
            ConfigurationHash = configurationHash,
            BaselineConfigurationId = baselineConfigurationId,
            BaselineConfigurationHash = baselineConfigurationHash,
            CandidateSnapshotId = candidateSnapshotId,
            EvaluatorVersion = evaluatorVersion,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            ControlProducts = controlProducts,
            ControlValues = controlValues,
            Improvements = improvements,
            Regressions = regressions,
            CandidateCorrect = candidateCorrect,
            CandidateIncorrect = candidateIncorrect,
            IsReady = isReady,
            SnapshotJson = snapshotJson
        };
    }
}
