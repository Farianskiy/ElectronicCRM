using System.Globalization;
using ElectronicService.Domain.Catalog.Characteristics;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionRuleSetCurrentValueComparer
{
    public static IReadOnlyList<CatalogRecognitionRuleSetFieldComparison> Compare(
        CatalogRecognitionRuleSetNamePreviewResult preview,
        IReadOnlyDictionary<string, string> currentValues)
    {
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(currentValues);

        var proposals = preview.Resolution.Characteristics
            .ToDictionary(item => item.CharacteristicDefinitionId);

        var comparisons =
            new List<CatalogRecognitionRuleSetFieldComparison>(
                preview.CharacteristicDefinitions.Count);

        foreach (var definition in preview.CharacteristicDefinitions)
        {
            currentValues.TryGetValue(
                definition.CharacteristicDefinitionId.ToString(),
                out var currentValue);

            proposals.TryGetValue(
                definition.CharacteristicDefinitionId,
                out var proposal);

            var status = GetStatus(
                definition.DataType,
                currentValue,
                proposal);

            comparisons.Add(new CatalogRecognitionRuleSetFieldComparison(
                definition.CharacteristicDefinitionId,
                definition.Name,
                definition.Unit,
                currentValue,
                proposal?.ProposedValue,
                status));
        }

        return comparisons;
    }

    private static string GetStatus(
        CharacteristicDataType dataType,
        string? currentValue,
        CatalogRecognitionResolvedCharacteristic? proposal)
    {
        if (proposal is null)
        {
            return "NoProposal";
        }

        if (proposal.HasConflict)
        {
            return "RuleConflict";
        }

        if (proposal.ProposedValue is not string proposedValue)
        {
            return "NoProposal";
        }

        if (string.IsNullOrWhiteSpace(currentValue))
        {
            return "SuggestedValue";
        }

        if (dataType == CharacteristicDataType.Number)
        {
            const NumberStyles styles =
                NumberStyles.AllowLeadingSign |
                NumberStyles.AllowDecimalPoint;

            if (!decimal.TryParse(
                    currentValue,
                    styles,
                    CultureInfo.InvariantCulture,
                    out var currentNumber) ||
                !decimal.TryParse(
                    proposedValue,
                    styles,
                    CultureInfo.InvariantCulture,
                    out var proposedNumber))
            {
                return "CurrentValueNotComparable";
            }

            return MatchStatus(currentNumber == proposedNumber);
        }

        if (dataType == CharacteristicDataType.Boolean)
        {
            if (!bool.TryParse(currentValue, out var currentFlag) ||
                !bool.TryParse(proposedValue, out var proposedFlag))
            {
                return "CurrentValueNotComparable";
            }

            return MatchStatus(currentFlag == proposedFlag);
        }

        if (dataType == CharacteristicDataType.Text)
        {
            return MatchStatus(string.Equals(
                currentValue,
                proposedValue,
                StringComparison.Ordinal));
        }

        return "CurrentValueNotComparable";
    }

    private static string MatchStatus(bool matches)
    {
        return matches
            ? "MatchesCurrentValue"
            : "DiffersFromCurrentValue";
    }
}