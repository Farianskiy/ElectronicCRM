namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionLiteralProposalGenerator
{
    public const string Version = "literal-v1";

    public static CatalogRecognitionLiteralProposalSet Generate(CatalogRecognitionPreparedSampleSet source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!source.CanGenerate)
        {
            return new CatalogRecognitionLiteralProposalSet(source.Scope, Version, Array.Empty<CatalogRecognitionLiteralProposal>(), source.Issues);
        }

        var proposals = new List<CatalogRecognitionLiteralProposal>();
        var groups = source.Samples.GroupBy(sample => (sample.RawValue, sample.NormalizedValue)).OrderBy(group => group.Key.RawValue, StringComparer.Ordinal).ThenBy(group => group.Key.NormalizedValue, StringComparer.Ordinal);

        foreach (var key in groups.Select(group => group.Key))
        {
            var proposal = Evaluate(source.Samples, key.RawValue, key.NormalizedValue);
            proposals.Add(proposal);
        }

        return new CatalogRecognitionLiteralProposalSet(source.Scope, Version, proposals, source.Issues);
    }

    private static CatalogRecognitionLiteralProposal Evaluate(IReadOnlyList<CatalogRecognitionPreparedSample> samples, string literal, string normalizedValue)
    {
        var supportingIds = new HashSet<Guid>();
        var conflictingIds = new HashSet<Guid>();
        var matchedNameCount = 0;

        foreach (var sample in samples)
        {
            var positions = CatalogRecognitionLiteralMatcher.FindMatches(sample.ProductName, literal);
            var isSource = string.Equals(sample.RawValue, literal, StringComparison.Ordinal) && string.Equals(sample.NormalizedValue, normalizedValue, StringComparison.Ordinal);

            if (positions.Count == 0)
            {
                if (isSource)
                {
                    conflictingIds.UnionWith(sample.ExampleIds);
                }

                continue;
            }

            matchedNameCount++;

            var matchesAnnotation = positions.Count == 1
                && positions[0] == sample.SpanStart
                && literal.Length == sample.SpanLength
                && string.Equals(sample.RawValue, literal, StringComparison.Ordinal)
                && string.Equals(sample.NormalizedValue, normalizedValue, StringComparison.Ordinal);

            if (!matchesAnnotation)
            {
                conflictingIds.UnionWith(sample.ExampleIds);
                continue;
            }

            supportingIds.UnionWith(sample.ExampleIds);
        }

        return new CatalogRecognitionLiteralProposal(
            literal,
            normalizedValue,
            matchedNameCount,
            supportingIds.OrderBy(id => id).ToArray(),
            conflictingIds.OrderBy(id => id).ToArray());
    }
}