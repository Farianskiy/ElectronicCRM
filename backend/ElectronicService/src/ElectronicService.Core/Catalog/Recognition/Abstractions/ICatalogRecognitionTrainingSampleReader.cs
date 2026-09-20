using ElectronicService.Core.Catalog.Recognition.Training;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionTrainingSampleReader
{
    Task<CatalogRecognitionTrainingSampleSet> ReadAsync(CatalogRecognitionTrainingScope scope, int limit = 1000, CancellationToken cancellationToken = default);
}