using ElectronicService.Core.Catalog.Recognition.Abstractions;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed class CatalogRecognitionLiteralDraftRecheckService
{
    private readonly ICatalogRecognitionLiteralDraftReader _draftReader;
    private readonly ICatalogRecognitionTrainingSampleReader _sampleReader;

    public CatalogRecognitionLiteralDraftRecheckService(
        ICatalogRecognitionLiteralDraftReader draftReader,
        ICatalogRecognitionTrainingSampleReader sampleReader)
    {
        _draftReader = draftReader;
        _sampleReader = sampleReader;
    }

    public async Task<CatalogRecognitionLiteralDraftRecheckResult?> RecheckAsync(
        Guid draftId,
        CancellationToken cancellationToken = default)
    {
        var details = await _draftReader.GetByIdAsync(draftId, cancellationToken).ConfigureAwait(false);

        if (details is null)
        {
            return null;
        }

        var draft = details.Draft;
        var scope = new CatalogRecognitionTrainingScope(draft.ManufacturerId, draft.ProductTypeId, draft.CharacteristicDefinitionId);
        var source = await _sampleReader.ReadAsync(scope, cancellationToken: cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var prepared = CatalogRecognitionTrainingSamplePreparer.Prepare(source);
        var generated = CatalogRecognitionLiteralProposalGenerator.Generate(prepared);
        var proposal = generated.Proposals.FirstOrDefault(item => string.Equals(item.Literal, draft.Literal, StringComparison.Ordinal) && string.Equals(item.NormalizedValue, draft.NormalizedValue, StringComparison.Ordinal));
        var storedIds = details.Evidence.Select(item => item.ExampleId).ToHashSet();
        var currentIds = source.Samples.Select(item => item.ExampleId).ToHashSet();
        var selectionComplete = !source.HasMore;
        var versionMatches = string.Equals(draft.GeneratorVersion, generated.GeneratorVersion, StringComparison.Ordinal);

        Guid[] addedIds = [];
        Guid[] missingIds = [];
        var evidenceUnchanged = false;

        if (selectionComplete)
        {
            addedIds = currentIds.Except(storedIds).OrderBy(id => id).ToArray();
            missingIds = storedIds.Except(currentIds).OrderBy(id => id).ToArray();
            evidenceUnchanged = storedIds.SetEquals(currentIds);
        }

        return new CatalogRecognitionLiteralDraftRecheckResult(
            draft.Id,
            DateTime.UtcNow,
            generated.GeneratorVersion,
            versionMatches,
            selectionComplete,
            evidenceUnchanged,
            source.Samples.Count,
            addedIds,
            missingIds,
            proposal,
            generated.Issues);
    }
}