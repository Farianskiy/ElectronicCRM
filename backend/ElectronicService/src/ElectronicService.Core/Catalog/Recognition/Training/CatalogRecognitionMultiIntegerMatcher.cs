using System.Globalization;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionMultiIntegerMatcher
{
    public static CatalogRecognitionMultiIntegerMatchResult Match(
        string? productName,
        CatalogRecognitionMultiIntegerPattern? pattern)
    {
        if (pattern is null || !CatalogRecognitionMultiIntegerPatternValidator.IsValid(pattern))
        {
            return new CatalogRecognitionMultiIntegerMatchResult("InvalidPattern", []);
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            return new CatalogRecognitionMultiIntegerMatchResult("MissingName", []);
        }

        if (productName.Length > 2000)
        {
            return new CatalogRecognitionMultiIntegerMatchResult("NameTooLong", []);
        }

        var tokens = CatalogRecognitionNameTokenizer.Tokenize(productName);

        if (tokens.Count != pattern.Parts.Count)
        {
            return new CatalogRecognitionMultiIntegerMatchResult("NoMatch", []);
        }

        var captures = new List<CatalogRecognitionMultiIntegerCapture>();

        for (var index = 0; index < pattern.Parts.Count; index++)
        {
            var part = pattern.Parts[index];
            var token = tokens[index];

            if (part.CharacteristicDefinitionId is not Guid characteristicId)
            {
                if (!string.Equals(part.Literal, token.Text, StringComparison.Ordinal))
                {
                    return new CatalogRecognitionMultiIntegerMatchResult("NoMatch", []);
                }

                continue;
            }

            if (token.Kind != CatalogRecognitionNameTokenKind.Digits)
            {
                return new CatalogRecognitionMultiIntegerMatchResult("NoMatch", []);
            }

            var normalizedValue = CatalogRecognitionIntegerPatternMatcher.NormalizeInteger(token.Text);

            if (normalizedValue is null)
            {
                return new CatalogRecognitionMultiIntegerMatchResult("NoMatch", []);
            }

            if (!decimal.TryParse(normalizedValue, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            {
                return new CatalogRecognitionMultiIntegerMatchResult("NumberOutOfRange", []);
            }

            captures.Add(new CatalogRecognitionMultiIntegerCapture(
                characteristicId,
                token.Text,
                normalizedValue,
                token.Start,
                token.Length));
        }

        return new CatalogRecognitionMultiIntegerMatchResult("Matched", captures);
    }
}