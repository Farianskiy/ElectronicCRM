using System.Text;

namespace ElectronicService.Core.Catalog.Products.Analogies;

public static class CatalogProductFamilyAnchorExtractor
{
    private const int MinimumLetterPrefixLength = 2;
    private const int MaximumSeparatedLetterPrefixLength = 8;
    private const int MaximumSeparatedNumberLength = 4;

    public static IReadOnlyCollection<string> Extract(
        string? productName,
        string? article = null)
    {
        var anchors = new HashSet<string>(StringComparer.Ordinal);

        AddAnchors(productName, anchors);
        AddAnchors(article, anchors);

        return anchors
            .OrderByDescending(anchor => anchor.Length)
            .ThenBy(anchor => anchor, StringComparer.Ordinal)
            .ToArray();
    }

    private static void AddAnchors(
        string? source,
        HashSet<string> anchors)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return;
        }

        var tokens = Tokenize(source);
        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index];
            AddCombinedTokenAnchors(token, anchors);

            if (index + 1 >= tokens.Length
                || !IsLetterToken(token)
                || token.Length > MaximumSeparatedLetterPrefixLength
                || !IsNumberToken(tokens[index + 1])
                || tokens[index + 1].Length > MaximumSeparatedNumberLength)
            {
                continue;
            }

            anchors.Add(token + tokens[index + 1]);
        }
    }

    private static void AddCombinedTokenAnchors(
        string token,
        HashSet<string> anchors)
    {
        var canonical = Canonicalize(token);
        if (!HasFamilyShape(canonical))
        {
            return;
        }

        anchors.Add(canonical);

        var separatorIndex = token.IndexOf('-', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return;
        }

        var prefix = Canonicalize(token[..separatorIndex]);
        if (HasFamilyShape(prefix))
        {
            anchors.Add(prefix);
        }
    }

    private static bool HasFamilyShape(string value)
    {
        if (value.Length < MinimumLetterPrefixLength + 1)
        {
            return false;
        }

        var letterPrefixLength = 0;
        while (letterPrefixLength < value.Length
               && char.IsLetter(value[letterPrefixLength]))
        {
            letterPrefixLength++;
        }

        return letterPrefixLength >= MinimumLetterPrefixLength
            && value.Skip(letterPrefixLength).Any(char.IsDigit);
    }

    private static string[] Tokenize(string value)
    {
        var normalized = new StringBuilder(value.Length);
        foreach (var sourceCharacter in value)
        {
            var character = sourceCharacter switch
            {
                'ё' or 'Ё' => 'Е',
                _ => char.ToUpperInvariant(sourceCharacter)
            };

            normalized.Append(char.IsLetterOrDigit(character) || character == '-'
                ? character
                : ' ');
        }

        return normalized
            .ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string Canonicalize(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit));

    private static bool IsLetterToken(string value) =>
        value.Length >= MinimumLetterPrefixLength
        && value.All(char.IsLetter);

    private static bool IsNumberToken(string value) =>
        value.Length > 0
        && value.All(char.IsDigit);
}
