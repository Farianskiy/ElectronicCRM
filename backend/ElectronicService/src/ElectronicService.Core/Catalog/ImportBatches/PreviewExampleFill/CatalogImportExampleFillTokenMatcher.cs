namespace ElectronicService.Core.Catalog.ImportBatches.PreviewExampleFill;

public static class CatalogImportExampleFillTokenMatcher
{
    public static int FindStart(string productName, string rawValue)
    {
        ArgumentNullException.ThrowIfNull(productName);
        ArgumentNullException.ThrowIfNull(rawValue);

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return -1;
        }

        var startIndex = 0;
        while (startIndex <= productName.Length - rawValue.Length)
        {
            var index = productName.IndexOf(
                rawValue,
                startIndex,
                StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return -1;
            }

            var beforeIsBoundary = index == 0
                || !char.IsLetterOrDigit(productName[index - 1])
                || !char.IsLetterOrDigit(rawValue[0]);
            var end = index + rawValue.Length;
            var afterIsBoundary = end == productName.Length
                || !char.IsLetterOrDigit(productName[end])
                || !char.IsLetterOrDigit(rawValue[^1]);

            if (beforeIsBoundary && afterIsBoundary)
            {
                return index;
            }

            startIndex = index + 1;
        }

        return -1;
    }
}
