namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionIntegerPatternMatcher
{
    public static CatalogRecognitionIntegerCapture? Match(
        string productName,
        CatalogRecognitionIntegerPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(productName);
        ArgumentNullException.ThrowIfNull(pattern);

        if (productName.Length > 2000 || pattern.Prefix.Length > 2000 || pattern.Suffix.Length > 2000)
        {
            return null;
        }

        if (pattern.Prefix.Length == 0 && pattern.Suffix.Length == 0)
        {
            return null;
        }

        if (!productName.StartsWith(pattern.Prefix, StringComparison.Ordinal) || !productName.EndsWith(pattern.Suffix, StringComparison.Ordinal))
        {
            return null;
        }

        var length = productName.Length - pattern.Prefix.Length - pattern.Suffix.Length;

        if (length <= 0)
        {
            return null;
        }

        var rawValue = productName.Substring(pattern.Prefix.Length, length);
        var normalizedValue = NormalizeInteger(rawValue);

        if (normalizedValue is null)
        {
            return null;
        }

        var tokens = CatalogRecognitionNameTokenizer.Tokenize(productName);
        var span = CatalogRecognitionTokenSpanResolver.Resolve(tokens, pattern.Prefix.Length, length);

        if (span is null || span.TokenCount != 1 || tokens[span.FirstTokenIndex].Kind != CatalogRecognitionNameTokenKind.Digits)
        {
            return null;
        }

        return new CatalogRecognitionIntegerCapture(rawValue, normalizedValue, pattern.Prefix.Length, length);
    }

    public static string? NormalizeInteger(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0 || value.Length > 2000 || value.Any(character => character is < '0' or > '9'))
        {
            return null;
        }

        var normalized = value.TrimStart('0');

        return normalized.Length == 0 ? "0" : normalized;
    }
}