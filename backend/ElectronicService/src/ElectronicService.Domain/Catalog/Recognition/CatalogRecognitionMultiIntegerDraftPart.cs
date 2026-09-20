namespace ElectronicService.Domain.Catalog.Recognition;

public sealed class CatalogRecognitionMultiIntegerDraftPart
{
    private CatalogRecognitionMultiIntegerDraftPart()
    {
    }

    internal CatalogRecognitionMultiIntegerDraftPart(
        Guid draftId,
        int position,
        string? literal,
        Guid? characteristicDefinitionId,
        int distinctValueCount)
    {
        DraftId = draftId;
        Position = position;
        Literal = literal;
        CharacteristicDefinitionId = characteristicDefinitionId;
        DistinctValueCount = distinctValueCount;
    }

    public Guid DraftId { get; private set; }
    public int Position { get; private set; }
    public string? Literal { get; private set; }
    public Guid? CharacteristicDefinitionId { get; private set; }
    public int DistinctValueCount { get; private set; }
}