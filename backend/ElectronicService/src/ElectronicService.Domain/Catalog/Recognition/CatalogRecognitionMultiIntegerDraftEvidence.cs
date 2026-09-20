namespace ElectronicService.Domain.Catalog.Recognition;

public sealed class CatalogRecognitionMultiIntegerDraftEvidence
{
    private CatalogRecognitionMultiIntegerDraftEvidence()
    {
    }

    internal CatalogRecognitionMultiIntegerDraftEvidence(
        Guid draftId,
        Guid trainingExampleId,
        bool isSupporting)
    {
        DraftId = draftId;
        TrainingExampleId = trainingExampleId;
        IsSupporting = isSupporting;
    }

    public Guid DraftId { get; private set; }
    public Guid TrainingExampleId { get; private set; }
    public bool IsSupporting { get; private set; }
}