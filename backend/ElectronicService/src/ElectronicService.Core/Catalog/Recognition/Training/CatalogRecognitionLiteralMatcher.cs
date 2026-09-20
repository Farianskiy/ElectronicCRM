namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionLiteralMatcher
{
    public static IReadOnlyList<int> FindMatches(string productName, string literal)
    {
        ArgumentNullException.ThrowIfNull(productName);
        ArgumentNullException.ThrowIfNull(literal);

        if (string.IsNullOrWhiteSpace(literal))
        {
            return Array.Empty<int>();
        }

        var matches = new List<int>();
        var searchStart = 0;

        while (searchStart <= productName.Length - literal.Length)
        {
            var position = productName.IndexOf(literal, searchStart, StringComparison.Ordinal);

            if (position < 0)
            {
                break;
            }

            var end = position + literal.Length;
            var leftBoundary = position == 0 || !IsTokenCharacter(productName[position - 1]);
            var rightBoundary = end == productName.Length || !IsTokenCharacter(productName[end]);

            if (leftBoundary && rightBoundary)
            {
                matches.Add(position);
            }

            searchStart = position + 1;
        }

        return matches;
    }

    private static bool IsTokenCharacter(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_' || value == '+';
    }
}