using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.Analogies;

public sealed record CatalogProductAnalogyEvaluationRow(
    Guid ProductId,
    string ProductName,
    string CharacteristicCode,
    string ExpectedValue,
    string? BaselineValue,
    string? CandidateValue,
    bool BaselineCorrect,
    bool CandidateCorrect);

public sealed record CatalogProductAnalogyEvaluationSnapshot(
    int FormatVersion,
    string EvaluatorVersion,
    Guid ConfigurationId,
    string ConfigurationHash,
    Guid? BaselineConfigurationId,
    string BaselineConfigurationHash,
    string CandidateSnapshotId,
    IReadOnlyCollection<CatalogProductAnalogyEvaluationRow> Rows,
    IReadOnlyCollection<string> BlockingReasons);

public sealed record CatalogProductAnalogyEvaluationReportView(
    Guid Id,
    Guid ConfigurationId,
    DateTime CreatedAtUtc,
    int ControlProducts,
    int ControlValues,
    int Improvements,
    int Regressions,
    int CandidateCorrect,
    int CandidateIncorrect,
    bool IsReady,
    bool IsCurrent,
    IReadOnlyCollection<string> BlockingReasons);

public sealed record CatalogProductAnalogyActivationResult(
    Guid ConfigurationId,
    int VersionNumber,
    string Status,
    DateTime? ActivatedAtUtc);

public sealed record CatalogProductAnalogyEvaluationReportDetail(
    CatalogProductAnalogyEvaluationReportView Report,
    IReadOnlyCollection<CatalogProductAnalogyEvaluationRow> Rows);

public sealed class CatalogProductAnalogyEvaluationService(
    ICatalogProductAnalogyConfigurationRepository repository,
    ICatalogProductAnalogyReader reader,
    CatalogProductAnalogyConfigurationResolver resolver)
{
    public const string EvaluatorVersion = "catalog-analogy-evaluation-v1";
    public const int FormatVersion = 1;
    private const int MaximumControlProducts = 500;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<Result<CatalogProductAnalogyEvaluationReportView, DomainError>> EvaluateAsync(
        Guid configurationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var configuration = await repository.GetByIdAsync(configurationId, cancellationToken)
            .ConfigureAwait(false);
        if (configuration is null)
            return new DomainError("catalog_analogy.not_found", "Версия конфигурации не найдена.");
        if (configuration.Status is not (CatalogProductAnalogyConfigurationStatus.Draft
            or CatalogProductAnalogyConfigurationStatus.Evaluated))
            return new DomainError("catalog_analogy.invalid_status", "Эту версию нельзя оценивать.");

        var candidateSettings = CatalogProductAnalogySettingsCodec.Deserialize(configuration.SettingsJson);
        if (candidateSettings.IsFailure) return candidateSettings.Error;
        var baseline = await resolver.ResolveAsync(configuration.ManufacturerId,
            configuration.ProductTypeId, cancellationToken).ConfigureAwait(false);
        if (baseline.IsFailure) return baseline.Error;

        var poolSize = Math.Max(candidateSettings.Value.MaximumCandidatePoolSize,
            baseline.Value.Settings.MaximumCandidatePoolSize);
        var source = await reader.ReadCandidateSnapshotAsync(configuration.ManufacturerId,
            configuration.ProductTypeId, poolSize, cancellationToken).ConfigureAwait(false);
        var controls = source.Candidates
            .Where(x => x.Characteristics.Count > 0)
            .OrderBy(x => x.ProductId)
            .Take(MaximumControlProducts)
            .ToArray();

        var rows = new List<CatalogProductAnalogyEvaluationRow>();
        foreach (var control in controls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = new CatalogProductAnalogyRequest(control.Name, control.Article,
                control.ManufacturerId, control.ProductTypeId,
                new Dictionary<string, string>(StringComparer.Ordinal),
                control.ProductId, MaximumResults: 100);
            var baselineResult = CatalogProductAnalogyEvaluator.Evaluate(request, source.Candidates,
                baseline.Value.Settings);
            var candidateResult = CatalogProductAnalogyEvaluator.Evaluate(request, source.Candidates,
                candidateSettings.Value);
            var baselineProposals = baselineResult.CharacteristicProposals
                .Where(x => x.IsSuggested).ToDictionary(x => x.CharacteristicCode,
                    x => x.ProposedValue, StringComparer.OrdinalIgnoreCase);
            var candidateProposals = candidateResult.CharacteristicProposals
                .Where(x => x.IsSuggested).ToDictionary(x => x.CharacteristicCode,
                    x => x.ProposedValue, StringComparer.OrdinalIgnoreCase);

            foreach (var expected in control.Characteristics.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                baselineProposals.TryGetValue(expected.Key, out var baselineValue);
                candidateProposals.TryGetValue(expected.Key, out var candidateValue);
                rows.Add(new CatalogProductAnalogyEvaluationRow(control.ProductId, control.Name,
                    expected.Key, expected.Value, baselineValue, candidateValue,
                    EqualValue(baselineValue, expected.Value), EqualValue(candidateValue, expected.Value)));
            }
        }

        var improvements = rows.Count(x => !x.BaselineCorrect && x.CandidateCorrect);
        var regressions = rows.Count(x => x.BaselineCorrect && !x.CandidateCorrect);
        var candidateCorrect = rows.Count(x => x.CandidateCorrect);
        var candidateIncorrect = rows.Count(x => x.CandidateValue is not null && !x.CandidateCorrect);
        var reasons = new List<string>();
        if (controls.Length < 3) reasons.Add("Нужно минимум три контрольных товара с характеристиками.");
        if (rows.Count < 3) reasons.Add("Нужно минимум три контрольных значения.");
        if (regressions > 0) reasons.Add($"Обнаружено регрессий: {regressions}.");
        if (improvements == 0) reasons.Add("Версия не даёт подтверждённых улучшений относительно текущей конфигурации.");
        if (candidateIncorrect > rows.Count(x => x.BaselineValue is not null && !x.BaselineCorrect))
            reasons.Add("Количество неправильных предложений увеличилось.");

        var snapshot = new CatalogProductAnalogyEvaluationSnapshot(FormatVersion, EvaluatorVersion,
            configuration.Id, configuration.SettingsHash, baseline.Value.ConfigurationId,
            baseline.Value.SettingsHash, source.SnapshotId, rows, reasons);
        var report = CatalogProductAnalogyEvaluationReport.Create(configuration.Id,
            configuration.SettingsHash, baseline.Value.ConfigurationId, baseline.Value.SettingsHash,
            source.SnapshotId, EvaluatorVersion, userId, controls.Length, rows.Count,
            improvements, regressions, candidateCorrect, candidateIncorrect,
            reasons.Count == 0, JsonSerializer.Serialize(snapshot, JsonOptions));
        if (report.IsFailure) return report.Error;

        var marked = configuration.MarkEvaluated();
        if (marked.IsFailure) return marked.Error;
        repository.Add(report.Value);
        if (!await repository.TrySaveChangesAsync(cancellationToken).ConfigureAwait(false))
            return new DomainError("catalog_analogy.concurrency", "Версия изменилась во время оценки. Запустите оценку повторно.");
        return Map(report.Value, true, reasons);
    }

    public async Task<Result<IReadOnlyCollection<CatalogProductAnalogyEvaluationReportView>, DomainError>>
        ListAsync(Guid configurationId, CancellationToken cancellationToken = default)
    {
        var configuration = await repository.GetByIdAsync(configurationId, cancellationToken).ConfigureAwait(false);
        if (configuration is null) return new DomainError("catalog_analogy.not_found", "Версия конфигурации не найдена.");
        var reports = await repository.ListReportsAsync(configurationId, cancellationToken).ConfigureAwait(false);
        var result = new List<CatalogProductAnalogyEvaluationReportView>();
        foreach (var report in reports)
        {
            var read = Read(report);
            if (read.IsFailure) return read.Error;
            result.Add(Map(report, await IsCurrentAsync(configuration, report, cancellationToken).ConfigureAwait(false), read.Value.BlockingReasons));
        }
        return result;
    }

    public async Task<Result<CatalogProductAnalogyActivationResult, DomainError>> ActivateAsync(
        Guid configurationId, Guid reportId, uint expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var configuration = await repository.GetByIdAsync(configurationId, cancellationToken).ConfigureAwait(false);
        var report = await repository.GetReportAsync(reportId, cancellationToken).ConfigureAwait(false);
        if (configuration is null || report is null || report.ConfigurationId != configurationId)
            return new DomainError("catalog_analogy.not_found", "Версия или отчёт оценки не найден.");
        if (!report.IsReady) return new DomainError("catalog_analogy.not_ready", "Отчёт не допускает выпуск версии.");
        if (!await IsCurrentAsync(configuration, report, cancellationToken).ConfigureAwait(false))
            return new DomainError("catalog_analogy.stale", "Конфигурация, действующая версия или контрольные товары изменились. Создайте новую оценку.");
        if (!await repository.TryActivateAsync(configuration, expectedVersion, cancellationToken).ConfigureAwait(false))
            return new DomainError("catalog_analogy.concurrency", "Версия была изменена одновременно. Обновите страницу.");
        return new CatalogProductAnalogyActivationResult(configuration.Id,
            configuration.VersionNumber, configuration.Status.ToString(), configuration.ActivatedAtUtc);
    }

    public async Task<Result<CatalogProductAnalogyEvaluationReportDetail, DomainError>> GetAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        var report = await repository.GetReportAsync(reportId, cancellationToken).ConfigureAwait(false);
        if (report is null) return new DomainError("catalog_analogy.not_found", "Отчёт оценки не найден.");
        var configuration = await repository.GetByIdAsync(report.ConfigurationId, cancellationToken).ConfigureAwait(false);
        if (configuration is null) return new DomainError("catalog_analogy.not_found", "Версия конфигурации не найдена.");
        var snapshot = Read(report);
        if (snapshot.IsFailure) return snapshot.Error;
        var current = await IsCurrentAsync(configuration, report, cancellationToken).ConfigureAwait(false);
        return new CatalogProductAnalogyEvaluationReportDetail(
            Map(report, current, snapshot.Value.BlockingReasons), snapshot.Value.Rows);
    }

    private async Task<bool> IsCurrentAsync(CatalogProductAnalogyConfiguration configuration,
        CatalogProductAnalogyEvaluationReport report, CancellationToken cancellationToken)
    {
        if (!string.Equals(configuration.SettingsHash, report.ConfigurationHash, StringComparison.Ordinal)) return false;
        var baseline = await resolver.ResolveAsync(configuration.ManufacturerId, configuration.ProductTypeId,
            cancellationToken).ConfigureAwait(false);
        if (baseline.IsFailure || baseline.Value.ConfigurationId != report.BaselineConfigurationId
            || !string.Equals(baseline.Value.SettingsHash, report.BaselineConfigurationHash, StringComparison.Ordinal)) return false;
        var settings = CatalogProductAnalogySettingsCodec.Deserialize(configuration.SettingsJson);
        if (settings.IsFailure) return false;
        var poolSize = Math.Max(settings.Value.MaximumCandidatePoolSize,
            baseline.Value.Settings.MaximumCandidatePoolSize);
        var current = await reader.ReadCandidateSnapshotAsync(configuration.ManufacturerId,
            configuration.ProductTypeId, poolSize, cancellationToken).ConfigureAwait(false);
        return string.Equals(current.SnapshotId, report.CandidateSnapshotId, StringComparison.Ordinal);
    }

    private static Result<CatalogProductAnalogyEvaluationSnapshot, DomainError> Read(
        CatalogProductAnalogyEvaluationReport report)
    {
        try
        {
            var value = JsonSerializer.Deserialize<CatalogProductAnalogyEvaluationSnapshot>(report.SnapshotJson, JsonOptions);
            return value is not null && value.FormatVersion == FormatVersion
                && string.Equals(value.EvaluatorVersion, EvaluatorVersion, StringComparison.Ordinal)
                ? value
                : new DomainError("catalog_analogy.invalid_report", "Формат отчёта оценки не поддерживается.");
        }
        catch (JsonException)
        {
            return new DomainError("catalog_analogy.invalid_report", "Сохранённый отчёт оценки повреждён.");
        }
    }

    private static CatalogProductAnalogyEvaluationReportView Map(
        CatalogProductAnalogyEvaluationReport report, bool current,
        IReadOnlyCollection<string> reasons) =>
        new(report.Id, report.ConfigurationId, report.CreatedAtUtc, report.ControlProducts,
            report.ControlValues, report.Improvements, report.Regressions,
            report.CandidateCorrect, report.CandidateIncorrect, report.IsReady,
            current, reasons);

    private static bool EqualValue(string? left, string right) =>
        !string.IsNullOrWhiteSpace(left)
        && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
