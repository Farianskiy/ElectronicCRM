namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionMultiIntegerPatternValidator
{
    public static bool IsValid(CatalogRecognitionMultiIntegerPattern? pattern)
    {
        if (pattern?.Parts is null || pattern.Parts.Count == 0 || pattern.Parts.Count > 256)
        {
            return false;
        }

        var characteristicIds = new HashSet<Guid>();
        var hasLiteral = false;
        var literalLength = 0;
        var previousWasCapture = false;

        foreach (var part in pattern.Parts)
        {
            if (part is null)
            {
                return false;
            }

            if (part.CharacteristicDefinitionId is Guid characteristicId)
            {
                if (characteristicId == Guid.Empty || part.Literal is not null || previousWasCapture || !characteristicIds.Add(characteristicId))
                {
                    return false;
                }

                if (characteristicIds.Count > 16)
                {
                    return false;
                }

                previousWasCapture = true;
                continue;
            }

            if (string.IsNullOrEmpty(part.Literal) || part.Literal.Length > 2000)
            {
                return false;
            }

            literalLength += part.Literal.Length;

            if (literalLength > 2000)
            {
                return false;
            }

            var tokens = CatalogRecognitionNameTokenizer.Tokenize(part.Literal);

            if (tokens.Count != 1)
            {
                return false;
            }

            hasLiteral = true;
            previousWasCapture = false;
        }

        return hasLiteral && characteristicIds.Count > 0;
    }
}