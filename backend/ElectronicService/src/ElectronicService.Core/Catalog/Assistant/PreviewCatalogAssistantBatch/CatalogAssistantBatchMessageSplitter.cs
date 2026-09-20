using System.Globalization;
using System.Text.RegularExpressions;

namespace ElectronicService.Core.Catalog.Assistant.PreviewCatalogAssistantBatch;

public sealed record CatalogAssistantBatchLineInput(int LineNumber, string SourceText, string SearchText, decimal? Quantity);

public sealed record CatalogAssistantBatchInput(string? CommonText, IReadOnlyCollection<CatalogAssistantBatchLineInput> Lines);

public static partial class CatalogAssistantBatchMessageSplitter
{
    private const int RegexTimeoutMilliseconds = 100;

    public static CatalogAssistantBatchInput Split(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var explicitSegments = SplitByExplicitSeparators(message);
        string? commonText = null;

        if (explicitSegments.Count > 1
            && !QuantityRegex().IsMatch(explicitSegments[0])
            && (explicitSegments.Skip(1).Any(QuantityRegex().IsMatch)
                || CommonTextPrefixRegex().IsMatch(explicitSegments[0])))
        {
            commonText = explicitSegments[0];
            explicitSegments.RemoveAt(0);
        }

        var sourceLines = explicitSegments
            .SelectMany(SplitByQuantityBoundaries)
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();

        var lines = sourceLines
            .Select((sourceText, index) => CreateLine(index + 1, sourceText, commonText))
            .ToArray();

        return new CatalogAssistantBatchInput(commonText, lines);
    }

    private static List<string> SplitByExplicitSeparators(string message)
    {
        return message
            .Split(['\r', '\n', ';', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToList();
    }

    private static IReadOnlyCollection<string> SplitByQuantityBoundaries(string segment)
    {
        var quantityMatches = QuantityRegex().Matches(segment).Cast<Match>().ToArray();

        if (quantityMatches.Length <= 1)
        {
            return [segment.Trim()];
        }

        var lines = new List<string>(quantityMatches.Length);
        var startIndex = 0;

        foreach (var quantityMatch in quantityMatches)
        {
            var length = quantityMatch.Index + quantityMatch.Length - startIndex;
            var line = segment.Substring(startIndex, length).Trim(' ', ',', ';', '/');

            if (line.Length > 0)
            {
                lines.Add(line);
            }

            startIndex = quantityMatch.Index + quantityMatch.Length;
        }

        var remainder = segment[startIndex..].Trim(' ', ',', ';', '/');

        if (remainder.Length > 0)
        {
            lines.Add(remainder);
        }

        return lines;
    }

    private static CatalogAssistantBatchLineInput CreateLine(int lineNumber, string sourceText, string? commonText)
    {
        var quantityMatch = QuantityRegex().Match(sourceText);
        decimal? quantity = null;
        var itemText = sourceText.Trim();

        if (quantityMatch.Success)
        {
            var normalizedQuantity = quantityMatch.Groups["quantity"].Value.Replace(',', '.');

            if (decimal.TryParse(normalizedQuantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedQuantity))
            {
                quantity = parsedQuantity;
            }

            itemText = string.Concat(sourceText.AsSpan(0, quantityMatch.Index), sourceText.AsSpan(quantityMatch.Index + quantityMatch.Length)).Trim(' ', ',', ';', '/');
        }

        var searchText = string.IsNullOrWhiteSpace(commonText)
            ? itemText
            : string.Concat(commonText, " ", itemText);

        return new CatalogAssistantBatchLineInput(lineNumber, sourceText.Trim(), searchText.Trim(), quantity);
    }

    [GeneratedRegex(@"(?<![\p{L}\p{N}])(?<quantity>\d+(?:[.,]\d+)?)\s*(?:ШТ(?:\.|УК(?:А|И)?)?|ЕД(?:\.|ИНИЦ(?:А|Ы)?)?|IN)(?![\p{L}\p{N}])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeoutMilliseconds)]
    private static partial Regex QuantityRegex();

    [GeneratedRegex(@"^\s*(?:НАЙДИ|НАЙТИ|ПОКАЖИ|ПОДБЕРИ|НУЖЕН|НУЖНА|НУЖНЫ|ИЩУ)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeoutMilliseconds)]
    private static partial Regex CommonTextPrefixRegex();
}