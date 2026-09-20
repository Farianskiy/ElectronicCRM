namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionIntegerAlternativesMatcher
{
    public static IReadOnlyList<CatalogRecognitionIntegerCapture> Match(
        string productName,
        CatalogRecognitionIntegerAlternativesPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(productName);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(pattern.Suffixes);

        if (pattern.Suffixes.Count == 0 || pattern.Suffixes.Count > 16)
        {
            return Array.Empty<CatalogRecognitionIntegerCapture>();
        }

        var captures = new HashSet<CatalogRecognitionIntegerCapture>();

        foreach (var suffix in pattern.Suffixes)
        {
            var capture = CatalogRecognitionIntegerPatternMatcher.Match(productName, new CatalogRecognitionIntegerPattern(pattern.Prefix, suffix));

            if (capture is not null)
            {
                captures.Add(capture);
            }
        }

        return captures.OrderBy(item => item.SpanStart).ThenBy(item => item.SpanLength).ToArray();
    }
}