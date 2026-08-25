using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Learning;

public static class CatalogRecognitionCandidateSuggestionPolicy
{
    public const int MinimumOccurrenceCount = 3;

    public const int MinimumCorrectedCount = 3;

    public const int MinimumDistinctProductCount = 3;

    public const int MaximumRejectedCount = 1;

    public const decimal MinimumSupportingEvidenceRatio = 0.80m;

    public static bool IsEligible(CatalogRecognitionCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (!candidate.IsAccumulating || candidate.HasSuggestion)
        {
            return false;
        }

        if (candidate.OccurrenceCount < MinimumOccurrenceCount)
        {
            return false;
        }

        if (candidate.CorrectedCount < MinimumCorrectedCount)
        {
            return false;
        }

        if (candidate.DistinctProductCount < MinimumDistinctProductCount)
        {
            return false;
        }

        if (candidate.RejectedCount > MaximumRejectedCount)
        {
            return false;
        }

        return CalculateConfidence(candidate) >= MinimumSupportingEvidenceRatio;
    }

    public static decimal CalculateConfidence(CatalogRecognitionCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (candidate.OccurrenceCount <= 0)
        {
            return 0m;
        }

        var supportingEvidenceCount = (decimal)candidate.AcceptedCount + candidate.CorrectedCount;
        var confidence = supportingEvidenceCount / candidate.OccurrenceCount;

        return decimal.Round(confidence, 4, MidpointRounding.AwayFromZero);
    }
}