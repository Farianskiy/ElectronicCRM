namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed class CatalogRecognitionLiteralProposalService
{
    private readonly CatalogRecognitionTrainingDataLoader _loader;

    public CatalogRecognitionLiteralProposalService(CatalogRecognitionTrainingDataLoader loader)
    {
        ArgumentNullException.ThrowIfNull(loader);
        _loader = loader;
    }

    public async Task<CatalogRecognitionLiteralProposalSet> GenerateAsync(CatalogRecognitionTrainingScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var prepared = await _loader.LoadAsync(scope, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        return CatalogRecognitionLiteralProposalGenerator.Generate(prepared);
    }
}