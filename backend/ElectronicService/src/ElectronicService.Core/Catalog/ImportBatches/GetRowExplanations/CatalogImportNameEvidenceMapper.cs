using ElectronicService.Core.Catalog.ProductNames.Explanation;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Core.Catalog.Recognition.Normalization;

namespace ElectronicService.Core.Catalog.ImportBatches.GetRowExplanations;

public static class CatalogImportNameEvidenceMapper
{
    public static CatalogProductNameEvidenceSpan? Map(
        string productName,
        CatalogRecognizedCharacteristic candidate)
    {
        var start = candidate.StartIndex;
        var length = candidate.Length;

        if (candidate.Source == CatalogRecognitionSource.Dictionary)
        {
            // Позиция каждого символа нормализованного текста
            // в исходном наименовании.
            var offsets = new List<int>();
            var index = 0;

            while (index < productName.Length)
            {
                if (!char.IsWhiteSpace(productName[index]))
                {
                    offsets.Add(index);
                    index++;
                    continue;
                }

                var whitespaceStart = index;

                while (index < productName.Length &&
                       char.IsWhiteSpace(productName[index]))
                {
                    index++;
                }

                if (offsets.Count > 0 &&
                    index < productName.Length)
                {
                    offsets.Add(whitespaceStart);
                }
            }

            var normalized =
                CatalogRecognitionTextNormalizer.NormalizeText(productName);

            if (normalized.Length != offsets.Count ||
                start < 0 ||
                length <= 0 ||
                start > offsets.Count - length)
            {
                return null;
            }

            var end = offsets[start + length - 1] + 1;

            start = offsets[start];
            length = end - start;
        }

        if (start < 0 ||
            length <= 0 ||
            start > productName.Length - length)
        {
            return null;
        }

        var rawValue = productName.Substring(start, length);

        if (string.IsNullOrWhiteSpace(rawValue) ||
            string.IsNullOrWhiteSpace(candidate.RawValue) ||
            !string.Equals(
                CatalogRecognitionTextNormalizer.NormalizeText(rawValue),
                CatalogRecognitionTextNormalizer.NormalizeText(
                    candidate.RawValue),
                StringComparison.Ordinal))
        {
            return null;
        }

        return new CatalogProductNameEvidenceSpan(
            CatalogProductNameEvidenceKind.Characteristic,
            candidate.CharacteristicCode,
            candidate.NormalizedValue,
            rawValue,
            candidate.Source.ToString(),
            candidate.Confidence,
            candidate.Priority,
            start,
            length);
    }
}