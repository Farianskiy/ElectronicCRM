using System.Globalization;
using System.Text;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionNameTokenizer
{
    public static IReadOnlyList<CatalogRecognitionNameToken> Tokenize(string productName)
    {
        ArgumentNullException.ThrowIfNull(productName);

        if (productName.Length > 2000)
        {
            throw new ArgumentException("Название не должно превышать 2000 символов.", nameof(productName));
        }

        var tokens = new List<CatalogRecognitionNameToken>();
        var position = 0;
        var tokenStart = 0;
        CatalogRecognitionNameTokenKind? currentKind = null;

        foreach (var rune in productName.EnumerateRunes())
        {
            var kind = GetKind(rune);

            if (currentKind.HasValue && (kind != currentKind.Value || kind == CatalogRecognitionNameTokenKind.Separator))
            {
                tokens.Add(new CatalogRecognitionNameToken(currentKind.Value, productName.Substring(tokenStart, position - tokenStart), tokenStart, position - tokenStart));
                tokenStart = position;
            }

            currentKind = kind;
            position += rune.Utf16SequenceLength;
        }

        if (currentKind.HasValue)
        {
            tokens.Add(new CatalogRecognitionNameToken(currentKind.Value, productName.Substring(tokenStart, position - tokenStart), tokenStart, position - tokenStart));
        }

        return tokens;
    }

    private static CatalogRecognitionNameTokenKind GetKind(Rune rune)
    {
        if (Rune.IsLetter(rune))
        {
            return CatalogRecognitionNameTokenKind.Letters;
        }

        var category = Rune.GetUnicodeCategory(rune);

        if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark)
        {
            return CatalogRecognitionNameTokenKind.Letters;
        }

        if (rune.Value is >= '0' and <= '9')
        {
            return CatalogRecognitionNameTokenKind.Digits;
        }

        if (Rune.IsWhiteSpace(rune))
        {
            return CatalogRecognitionNameTokenKind.Whitespace;
        }

        return CatalogRecognitionNameTokenKind.Separator;
    }
}