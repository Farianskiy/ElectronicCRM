using ElectronicService.Core.Catalog.Recognition.Abstractions;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed class CatalogRecognitionTrainingDataLoader
{
    private readonly ICatalogRecognitionTrainingSampleReader _reader;

    public CatalogRecognitionTrainingDataLoader(ICatalogRecognitionTrainingSampleReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        _reader = reader;
    }

    public async Task<CatalogRecognitionPreparedSampleSet> LoadAsync(CatalogRecognitionTrainingScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var source = await _reader.ReadAsync(scope, cancellationToken: cancellationToken).ConfigureAwait(false);

        return CatalogRecognitionTrainingSamplePreparer.Prepare(source);
    }
}