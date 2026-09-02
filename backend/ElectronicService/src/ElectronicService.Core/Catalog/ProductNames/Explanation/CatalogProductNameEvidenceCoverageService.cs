namespace ElectronicService.Core.Catalog.ProductNames.Explanation;

public sealed class CatalogProductNameEvidenceCoverageService :
    ICatalogProductNameEvidenceCoverageService
{
    public CatalogProductNameExplanationResult Explain(
        string productName,
        IReadOnlyCollection<CatalogProductNameEvidenceSpan> evidence)
    {
        ArgumentNullException.ThrowIfNull(productName);
        ArgumentNullException.ThrowIfNull(evidence);

        var normalizedEvidence =
            NormalizeEvidence(productName, evidence);

        var coveredPositions =
            BuildCoveredPositions(
                productName.Length,
                normalizedEvidence);

        var (
            meaningfulCharactersCount,
            coveredMeaningfulCharactersCount) =
            CountCoverage(productName, coveredPositions);

        var unexplainedSpans =
            FindUnexplainedSpans(
                productName,
                coveredPositions);

        var coverage =
            CalculateCoverage(
                meaningfulCharactersCount,
                coveredMeaningfulCharactersCount);

        return new CatalogProductNameExplanationResult(
            productName,
            normalizedEvidence,
            unexplainedSpans,
            meaningfulCharactersCount,
            coveredMeaningfulCharactersCount,
            coverage);
    }

    private static CatalogProductNameEvidenceSpan[] NormalizeEvidence(
        string productName,
        IReadOnlyCollection<CatalogProductNameEvidenceSpan> evidence)
    {
        var normalizedEvidence =
            new List<CatalogProductNameEvidenceSpan>();

        foreach (var span in evidence)
        {
            if (span.Kind == CatalogProductNameEvidenceKind.None ||
                span.Length <= 0)
            {
                continue;
            }

            var requestedEndIndex =
                (long)span.StartIndex + span.Length;

            if (span.StartIndex >= productName.Length ||
                requestedEndIndex <= 0)
            {
                continue;
            }

            var startIndex =
                Math.Max(0, span.StartIndex);

            var endIndex =
                (int)Math.Min(
                    productName.Length,
                    requestedEndIndex);

            if (endIndex <= startIndex)
            {
                continue;
            }

            var length =
                endIndex - startIndex;

            normalizedEvidence.Add(
                span with
                {
                    RawValue = productName.Substring(startIndex, length),
                    StartIndex = startIndex,
                    Length = length
                });
        }

        return normalizedEvidence
            .OrderBy(span => span.StartIndex)
            .ThenByDescending(span => span.Length)
            .ThenByDescending(span => span.Priority)
            .ThenBy(span => span.Kind)
            .ThenBy(
                span => span.TargetCode,
                StringComparer.Ordinal)
            .ThenBy(
                span => span.TargetValue,
                StringComparer.Ordinal)
            .ToArray();
    }

    private static bool[] BuildCoveredPositions(
        int productNameLength,
        CatalogProductNameEvidenceSpan[] evidence)
    {
        var coveredPositions =
            new bool[productNameLength];

        foreach (var span in evidence)
        {
            for (var index = span.StartIndex;
                 index < span.EndIndex;
                 index++)
            {
                coveredPositions[index] = true;
            }
        }

        return coveredPositions;
    }

    private static (int MeaningfulCount, int CoveredCount) CountCoverage(
        string productName,
        bool[] coveredPositions)
    {
        var meaningfulCount = 0;
        var coveredCount = 0;

        for (var index = 0;
             index < productName.Length;
             index++)
        {
            if (!IsMeaningfulCharacter(productName[index]))
            {
                continue;
            }

            meaningfulCount++;

            if (coveredPositions[index])
            {
                coveredCount++;
            }
        }

        return (meaningfulCount, coveredCount);
    }

    private static CatalogProductNameUnexplainedSpan[] FindUnexplainedSpans(
        string productName,
        bool[] coveredPositions)
    {
        var unexplainedSpans =
            new List<CatalogProductNameUnexplainedSpan>();

        var index = 0;

        while (index < productName.Length)
        {
            if (coveredPositions[index] ||
                !IsMeaningfulCharacter(productName[index]))
            {
                index++;
                continue;
            }

            var startIndex = index;
            index++;

            while (index < productName.Length &&
                   !coveredPositions[index] &&
                   IsUnexplainedTokenCharacter(productName[index]))
            {
                index++;
            }

            var endIndex = index;

            while (endIndex > startIndex &&
                   !IsMeaningfulCharacter(productName[endIndex - 1]))
            {
                endIndex--;
            }

            if (endIndex <= startIndex)
            {
                continue;
            }

            var length =
                endIndex - startIndex;

            unexplainedSpans.Add(
                new CatalogProductNameUnexplainedSpan(
                    productName.Substring(startIndex, length),
                    startIndex,
                    length));
        }

        return unexplainedSpans.ToArray();
    }

    private static decimal CalculateCoverage(
        int meaningfulCharactersCount,
        int coveredMeaningfulCharactersCount)
    {
        if (meaningfulCharactersCount == 0)
        {
            return 0.0000m;
        }

        return decimal.Round(
            (decimal)coveredMeaningfulCharactersCount /
            meaningfulCharactersCount,
            4,
            MidpointRounding.AwayFromZero);
    }

    private static bool IsMeaningfulCharacter(char character)
    {
        return char.IsLetterOrDigit(character);
    }

    private static bool IsUnexplainedTokenCharacter(char character)
    {
        return char.IsLetterOrDigit(character) ||
               character is '-' or '/' or '\\' or '.' or '+' or '_';
    }
}