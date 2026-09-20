namespace ElectronicService.Domain.Catalog.Recognition;

public sealed class CatalogRecognitionIntegerDraftSuffix
{
    private CatalogRecognitionIntegerDraftSuffix()
    {
    }

    internal CatalogRecognitionIntegerDraftSuffix(Guid draftId, int position, string text)
    {
        DraftId = draftId;
        Position = position;
        Text = text;
    }

    public Guid DraftId { get; private set; }
    public int Position { get; private set; }
    public string Text { get; private set; } = string.Empty;
}