using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.GetSuggestions;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Queries;

public sealed class CatalogAssistantDictionarySuggestionReader : ICatalogAssistantDictionarySuggestionReader
{
    private const int DefaultPageSize = 20;

    private const int MaxPageSize = 100;

    private const int MaximumEvidenceExamplesPerSuggestion = 5;

    private readonly ElectronicDbContext _dbContext;

    public CatalogAssistantDictionarySuggestionReader(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task<CatalogAssistantDictionarySuggestionsPageResult> GetSuggestionsAsync(
        CatalogAssistantDictionarySuggestionStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);

        var normalizedPageSize = pageSize <= 0
            ? DefaultPageSize
            : Math.Clamp(pageSize, 1, MaxPageSize);

        var suggestionsQuery = _dbContext.CatalogAssistantDictionarySuggestions
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            suggestionsQuery = suggestionsQuery.Where(suggestion => suggestion.Status == status.Value);
        }

        var totalCount = await suggestionsQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var itemsData = await (
            from suggestion in suggestionsQuery
            join productType in _dbContext.ProductTypes.AsNoTracking()
                on suggestion.ProductTypeId equals (Guid?)productType.Id into productTypes
            from productType in productTypes.DefaultIfEmpty()
            join characteristicDefinition in _dbContext.CharacteristicDefinitions.AsNoTracking()
                on suggestion.CharacteristicDefinitionId equals (Guid?)characteristicDefinition.Id into characteristicDefinitions
            from characteristicDefinition in characteristicDefinitions.DefaultIfEmpty()
            join approvedProductType in _dbContext.ProductTypes.AsNoTracking()
                on suggestion.ApprovedProductTypeId equals (Guid?)approvedProductType.Id into approvedProductTypes
            from approvedProductType in approvedProductTypes.DefaultIfEmpty()
            join approvedCharacteristicDefinition in _dbContext.CharacteristicDefinitions.AsNoTracking()
                on suggestion.ApprovedCharacteristicDefinitionId equals (Guid?)approvedCharacteristicDefinition.Id into approvedCharacteristicDefinitions
            from approvedCharacteristicDefinition in approvedCharacteristicDefinitions.DefaultIfEmpty()
            orderby suggestion.CreatedAtUtc descending
            select new CatalogAssistantDictionarySuggestionData(
                suggestion.Id,
                suggestion.OriginalMessage,
                suggestion.UnknownPhrase,
                suggestion.NormalizedUnknownPhrase,
                suggestion.SuggestedKind,
                suggestion.SuggestedTargetCode,
                suggestion.SuggestedTargetValue,
                suggestion.Confidence,
                suggestion.Source,
                suggestion.ProductTypeId,
                productType == null ? null : productType.Code,
                productType == null ? null : productType.Name,
                suggestion.CharacteristicDefinitionId,
                characteristicDefinition == null ? null : characteristicDefinition.Code,
                characteristicDefinition == null ? null : characteristicDefinition.Name,
                suggestion.OccurrenceCount,
                suggestion.AcceptedEvidenceCount,
                suggestion.CorrectedEvidenceCount,
                suggestion.RejectedEvidenceCount,
                suggestion.GeneratedAutomatically,
                suggestion.ApprovedPhrase,
                suggestion.ApprovedKind,
                suggestion.ApprovedTargetCode,
                suggestion.ApprovedTargetValue,
                suggestion.ApprovedProductTypeId,
                approvedProductType == null ? null : approvedProductType.Code,
                approvedProductType == null ? null : approvedProductType.Name,
                suggestion.ApprovedCharacteristicDefinitionId,
                approvedCharacteristicDefinition == null ? null : approvedCharacteristicDefinition.Code,
                approvedCharacteristicDefinition == null ? null : approvedCharacteristicDefinition.Name,
                suggestion.ApprovedPriority,
                suggestion.CreatedDictionaryTermId,
                suggestion.Status,
                suggestion.CreatedByUserId,
                suggestion.CreatedAtUtc,
                suggestion.ReviewedByUserId,
                suggestion.ReviewedAtUtc,
                suggestion.ReviewComment))
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var recognitionLearningSuggestionIds = itemsData
            .Where(suggestion => suggestion.Source == CatalogDictionarySuggestionSource.RecognitionLearning)
            .Select(suggestion => suggestion.Id)
            .ToArray();

        var evidenceExamplesBySuggestionId = await GetEvidenceExamplesAsync(
            recognitionLearningSuggestionIds,
            cancellationToken).ConfigureAwait(false);

        var items = itemsData.Select(suggestion =>
        {
            if (!evidenceExamplesBySuggestionId.TryGetValue(suggestion.Id, out var evidenceExamples))
            {
                evidenceExamples = [];
            }

            return MapToResult(suggestion, evidenceExamples);
        }).ToList();

        return new CatalogAssistantDictionarySuggestionsPageResult(
            items,
            normalizedPage,
            normalizedPageSize,
            totalCount);
    }

    private async Task<Dictionary<Guid, List<CatalogAssistantDictionarySuggestionEvidenceExampleResult>>> GetEvidenceExamplesAsync(
        Guid[] suggestionIds,
        CancellationToken cancellationToken)
    {
        if (suggestionIds.Length == 0)
        {
            return [];
        }

        var evidenceData = await (
            from candidate in _dbContext.CatalogRecognitionCandidates.AsNoTracking()
            join evidence in _dbContext.CatalogRecognitionCandidateEvidenceEntries.AsNoTracking()
                on candidate.Id equals evidence.CandidateId
            join feedback in _dbContext.CatalogRecognitionFeedbackEntries.AsNoTracking()
                on evidence.FeedbackId equals feedback.Id
            where candidate.SuggestionId.HasValue &&
                suggestionIds.Contains(candidate.SuggestionId.Value)
            orderby evidence.CreatedAtUtc descending
            select new CatalogAssistantDictionarySuggestionEvidenceData(
                candidate.SuggestionId!.Value,
                evidence.CreatedAtUtc,
                feedback.Id,
                feedback.ProductName,
                feedback.FeedbackType,
                feedback.SuggestedRawValue,
                feedback.SuggestedNormalizedValue,
                feedback.FinalNormalizedValue,
                feedback.SuggestedConfidence,
                feedback.SuggestedSource,
                feedback.SpanStart,
                feedback.SpanLength,
                feedback.LabelQuality,
                feedback.FinalizedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return evidenceData
            .GroupBy(evidence => evidence.SuggestionId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(evidence => evidence.EvidenceCreatedAtUtc)
                    .Take(MaximumEvidenceExamplesPerSuggestion)
                    .Select(MapEvidenceExample)
                    .ToList());
    }

    private static CatalogAssistantDictionarySuggestionResult MapToResult(
        CatalogAssistantDictionarySuggestionData suggestion,
        List<CatalogAssistantDictionarySuggestionEvidenceExampleResult> evidenceExamples)
    {
        return new CatalogAssistantDictionarySuggestionResult(
            suggestion.Id,
            suggestion.OriginalMessage,
            suggestion.UnknownPhrase,
            suggestion.NormalizedUnknownPhrase,
            suggestion.SuggestedKind.ToString(),
            suggestion.SuggestedTargetCode,
            suggestion.SuggestedTargetValue,
            suggestion.Confidence,
            suggestion.Source.ToString(),
            suggestion.ProductTypeId,
            suggestion.ProductTypeCode,
            suggestion.ProductTypeName,
            suggestion.CharacteristicDefinitionId,
            suggestion.CharacteristicCode,
            suggestion.CharacteristicName,
            suggestion.OccurrenceCount,
            suggestion.AcceptedEvidenceCount,
            suggestion.CorrectedEvidenceCount,
            suggestion.RejectedEvidenceCount,
            suggestion.GeneratedAutomatically,
            evidenceExamples,
            suggestion.ApprovedPhrase,
            suggestion.ApprovedKind?.ToString(),
            suggestion.ApprovedTargetCode,
            suggestion.ApprovedTargetValue,
            suggestion.ApprovedProductTypeId,
            suggestion.ApprovedProductTypeCode,
            suggestion.ApprovedProductTypeName,
            suggestion.ApprovedCharacteristicDefinitionId,
            suggestion.ApprovedCharacteristicCode,
            suggestion.ApprovedCharacteristicName,
            suggestion.ApprovedPriority,
            suggestion.CreatedDictionaryTermId,
            suggestion.Status.ToString(),
            suggestion.CreatedByUserId,
            suggestion.CreatedAtUtc,
            suggestion.ReviewedByUserId,
            suggestion.ReviewedAtUtc,
            suggestion.ReviewComment);
    }

    private static CatalogAssistantDictionarySuggestionEvidenceExampleResult MapEvidenceExample(CatalogAssistantDictionarySuggestionEvidenceData evidence)
    {
        return new CatalogAssistantDictionarySuggestionEvidenceExampleResult(
            evidence.FeedbackId,
            evidence.ProductName,
            evidence.FeedbackType.ToString(),
            evidence.SuggestedRawValue,
            evidence.SuggestedNormalizedValue,
            evidence.FinalNormalizedValue,
            evidence.SuggestedConfidence,
            evidence.SuggestedSource,
            evidence.SpanStart,
            evidence.SpanLength,
            evidence.LabelQuality.ToString(),
            evidence.FinalizedAtUtc);
    }

    private sealed record CatalogAssistantDictionarySuggestionData(
        Guid Id,
        string OriginalMessage,
        string UnknownPhrase,
        string NormalizedUnknownPhrase,
        CatalogDictionaryTermKind SuggestedKind,
        string? SuggestedTargetCode,
        string SuggestedTargetValue,
        decimal Confidence,
        CatalogDictionarySuggestionSource Source,
        Guid? ProductTypeId,
        string? ProductTypeCode,
        string? ProductTypeName,
        Guid? CharacteristicDefinitionId,
        string? CharacteristicCode,
        string? CharacteristicName,
        int OccurrenceCount,
        int AcceptedEvidenceCount,
        int CorrectedEvidenceCount,
        int RejectedEvidenceCount,
        bool GeneratedAutomatically,
        string? ApprovedPhrase,
        CatalogDictionaryTermKind? ApprovedKind,
        string? ApprovedTargetCode,
        string? ApprovedTargetValue,
        Guid? ApprovedProductTypeId,
        string? ApprovedProductTypeCode,
        string? ApprovedProductTypeName,
        Guid? ApprovedCharacteristicDefinitionId,
        string? ApprovedCharacteristicCode,
        string? ApprovedCharacteristicName,
        int? ApprovedPriority,
        Guid? CreatedDictionaryTermId,
        CatalogAssistantDictionarySuggestionStatus Status,
        Guid CreatedByUserId,
        DateTime CreatedAtUtc,
        Guid? ReviewedByUserId,
        DateTime? ReviewedAtUtc,
        string? ReviewComment);

    private sealed record CatalogAssistantDictionarySuggestionEvidenceData(
        Guid SuggestionId,
        DateTime EvidenceCreatedAtUtc,
        Guid FeedbackId,
        string ProductName,
        CatalogRecognitionFeedbackType FeedbackType,
        string? SuggestedRawValue,
        string? SuggestedNormalizedValue,
        string? FinalNormalizedValue,
        decimal? SuggestedConfidence,
        string? SuggestedSource,
        int? SpanStart,
        int? SpanLength,
        CatalogRecognitionLabelQuality LabelQuality,
        DateTime? FinalizedAtUtc);
}