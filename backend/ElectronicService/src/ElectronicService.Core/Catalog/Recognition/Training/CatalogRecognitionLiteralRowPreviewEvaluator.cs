namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionLiteralRowPreviewEvaluator
{
    public static CatalogRecognitionLiteralRowPreviewResult Evaluate(
        CatalogRecognitionLiteralDraftListItem draft,
        CatalogRecognitionLiteralRowPreviewInput row)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(row);

        if (!string.Equals(draft.GeneratorVersion, CatalogRecognitionLiteralProposalGenerator.Version, StringComparison.Ordinal))
        {
            return CreateResult(row, "UnsupportedGeneratorVersion", null, Array.Empty<int>());
        }

        if (string.IsNullOrWhiteSpace(draft.Literal) || string.IsNullOrWhiteSpace(draft.NormalizedValue))
        {
            return CreateResult(row, "InvalidDraft", null, Array.Empty<int>());
        }

        if (!row.ManufacturerId.HasValue || row.ManufacturerId.Value == Guid.Empty || !row.ProductTypeId.HasValue || row.ProductTypeId.Value == Guid.Empty)
        {
            return CreateResult(row, "ScopeUnknown", null, Array.Empty<int>());
        }

        if (row.ManufacturerId.Value != draft.ManufacturerId || row.ProductTypeId.Value != draft.ProductTypeId)
        {
            return CreateResult(row, "OutsideScope", null, Array.Empty<int>());
        }

        if (string.IsNullOrWhiteSpace(row.ProductName))
        {
            return CreateResult(row, "MissingName", null, Array.Empty<int>());
        }

        var positions = CatalogRecognitionLiteralMatcher.FindMatches(row.ProductName, draft.Literal);

        if (positions.Count == 0)
        {
            return CreateResult(row, "NoMatch", null, positions);
        }

        if (positions.Count > 1)
        {
            return CreateResult(row, "AmbiguousMatch", null, positions);
        }

        if (string.IsNullOrWhiteSpace(row.CurrentValue))
        {
            return CreateResult(row, "SuggestedValue", draft.NormalizedValue, positions);
        }

        if (string.Equals(row.CurrentValue, draft.NormalizedValue, StringComparison.Ordinal))
        {
            return CreateResult(row, "MatchesCurrentValue", draft.NormalizedValue, positions);
        }

        return CreateResult(row, "DiffersFromCurrentValue", draft.NormalizedValue, positions);
    }

    private static CatalogRecognitionLiteralRowPreviewResult CreateResult(
        CatalogRecognitionLiteralRowPreviewInput row,
        string status,
        string? proposedValue,
        IReadOnlyList<int> positions)
    {
        return new CatalogRecognitionLiteralRowPreviewResult(
            row.RowId,
            row.RowNumber,
            row.ProductName,
            status,
            row.CurrentValue,
            proposedValue,
            positions);
    }
}