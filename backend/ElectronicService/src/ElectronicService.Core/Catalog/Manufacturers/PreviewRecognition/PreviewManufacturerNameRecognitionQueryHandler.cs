using ElectronicService.Core.Catalog.Manufacturers.Resolution;

namespace ElectronicService.Core.Catalog.Manufacturers.PreviewRecognition;

public sealed class PreviewManufacturerNameRecognitionQueryHandler
{
    private readonly IManufacturerResolver _manufacturerResolver;

    public PreviewManufacturerNameRecognitionQueryHandler(
        IManufacturerResolver manufacturerResolver)
    {
        ArgumentNullException.ThrowIfNull(manufacturerResolver);

        _manufacturerResolver = manufacturerResolver;
    }

    public async Task<ManufacturerNameRecognitionResult> Handle(
        PreviewManufacturerNameRecognitionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.ProductName);

        var resolutionIndex = await _manufacturerResolver
            .LoadIndexAsync(cancellationToken)
            .ConfigureAwait(false);

        return resolutionIndex.RecognizeInText(query.ProductName);
    }
}