namespace ElectronicService.Domain.Catalog.Recognition;

public sealed class CatalogRecognitionRuleSetEntry
{
    private CatalogRecognitionRuleSetEntry()
    {
    }

    internal CatalogRecognitionRuleSetEntry(
        Guid versionId,
        int position,
        CatalogRecognitionRuleSetEntryData data)
    {
        VersionId = versionId;
        Position = position;
        Kind = data.Kind;

        switch (data.Kind)
        {
            case CatalogRecognitionRuleKind.Literal:
                LiteralDraftId = data.DraftId;
                break;

            case CatalogRecognitionRuleKind.NumericCapture:
                IntegerDraftId = data.DraftId;
                break;

            case CatalogRecognitionRuleKind.MultipleNumericCaptures:
                MultiIntegerDraftId = data.DraftId;
                break;

            default:
                throw new ArgumentException("Неизвестный вид шаблона.", nameof(data));
        }
    }

    public Guid VersionId { get; private set; }
    public int Position { get; private set; }
    public CatalogRecognitionRuleKind Kind { get; private set; }
    public Guid? LiteralDraftId { get; private set; }
    public Guid? IntegerDraftId { get; private set; }
    public Guid? MultiIntegerDraftId { get; private set; }
}