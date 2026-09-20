namespace ElectronicService.Domain.Catalog.Recognition;

public sealed class CatalogRecognitionIntegerDraftEvidence
{
    private CatalogRecognitionIntegerDraftEvidence()
    {
    }

    internal CatalogRecognitionIntegerDraftEvidence(Guid draftId, Guid trainingExampleId, bool isSupporting)
    {
        DraftId = draftId;
        TrainingExampleId = trainingExampleId;
        IsSupporting = isSupporting;
    }

    public Guid DraftId { get; private set; }
    public Guid TrainingExampleId { get; private set; }
    public bool IsSupporting { get; private set; }
}