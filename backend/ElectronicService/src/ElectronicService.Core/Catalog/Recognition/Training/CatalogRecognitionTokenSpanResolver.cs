namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionTokenSpanResolver
{
    public static CatalogRecognitionTokenSpan? Resolve(
        IReadOnlyList<CatalogRecognitionNameToken> tokens,
        int spanStart,
        int spanLength)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        if (spanStart < 0 || spanLength <= 0 || spanStart > int.MaxValue - spanLength)
        {
            return null;
        }

        var spanEnd = spanStart + spanLength;
        var firstIndex = -1;

        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];

            if (token.Start == spanStart)
            {
                firstIndex = index;
            }

            if (firstIndex < 0)
            {
                continue;
            }

            var tokenEnd = token.Start + token.Length;

            if (tokenEnd == spanEnd)
            {
                return new CatalogRecognitionTokenSpan(firstIndex, index - firstIndex + 1);
            }

            if (tokenEnd > spanEnd)
            {
                return null;
            }
        }

        return null;
    }
}