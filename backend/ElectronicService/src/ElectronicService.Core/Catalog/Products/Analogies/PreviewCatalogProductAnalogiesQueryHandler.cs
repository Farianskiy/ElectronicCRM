namespace ElectronicService.Core.Catalog.Products.Analogies;

public sealed record CatalogProductAnalogyPreparedScope(
    Guid ManufacturerId,
    Guid? ProductTypeId,
    CatalogProductAnalogyConfigurationSnapshot Configuration,
    CatalogProductAnalogyCandidateSnapshot CandidateSnapshot);

public sealed class PreviewCatalogProductAnalogiesQueryHandler
{
    private readonly ICatalogProductAnalogyReader _reader;
    private readonly CatalogProductAnalogyConfigurationResolver? _configurationResolver;

    public PreviewCatalogProductAnalogiesQueryHandler(
        ICatalogProductAnalogyReader reader,
        CatalogProductAnalogyConfigurationResolver? configurationResolver = null)
    {
        ArgumentNullException.ThrowIfNull(reader);

        _reader = reader;
        _configurationResolver = configurationResolver;
    }

    public async Task<CatalogProductAnalogyResult> Handle(
        CatalogProductAnalogyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var scope = await PrepareScopeAsync(
                request.ManufacturerId,
                request.ProductTypeId,
                cancellationToken)
            .ConfigureAwait(false);

        return EvaluatePrepared(request, scope);
    }

    public async Task<CatalogProductAnalogyPreparedScope> PrepareScopeAsync(
        Guid manufacturerId,
        Guid? productTypeId,
        CancellationToken cancellationToken = default)
    {
        if (manufacturerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Укажите производителя.",
                nameof(manufacturerId));
        }

        CatalogProductAnalogyConfigurationSnapshot resolved;
        if (productTypeId.HasValue && _configurationResolver is not null)
        {
            var configuration = await _configurationResolver.ResolveAsync(
                manufacturerId,
                productTypeId.Value,
                cancellationToken).ConfigureAwait(false);
            if (configuration.IsFailure)
            {
                throw new InvalidOperationException(configuration.Error.Message);
            }

            resolved = configuration.Value;
        }
        else
        {
            resolved = new CatalogProductAnalogyConfigurationSnapshot(
                null,
                null,
                "Системная конфигурация",
                CatalogProductAnalogySettingsCodec.Normalize(
                    CatalogProductAnalogySettings.SystemDefault).Value.Hash,
                true,
                CatalogProductAnalogySettings.SystemDefault);
        }

        var snapshot = await _reader
            .ReadCandidateSnapshotAsync(
                manufacturerId,
                productTypeId,
                resolved.Settings.MaximumCandidatePoolSize,
                cancellationToken)
            .ConfigureAwait(false);

        return new CatalogProductAnalogyPreparedScope(
            manufacturerId,
            productTypeId,
            resolved,
            snapshot);
    }

    public static CatalogProductAnalogyResult EvaluatePrepared(
        CatalogProductAnalogyRequest request,
        CatalogProductAnalogyPreparedScope scope)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(scope);

        if (request.ManufacturerId != scope.ManufacturerId
            || request.ProductTypeId != scope.ProductTypeId)
        {
            throw new ArgumentException(
                "Запрос аналогов не совпадает с подготовленной областью.",
                nameof(request));
        }

        var result = CatalogProductAnalogyEvaluator.Evaluate(
            request,
            scope.CandidateSnapshot.Candidates,
            scope.Configuration.Settings);

        return result with
        {
            CandidateSnapshotId = scope.CandidateSnapshot.SnapshotId,
            CandidateSnapshotCreatedAtUtc = scope.CandidateSnapshot.CreatedAtUtc,
            ConfigurationId = scope.Configuration.ConfigurationId,
            ConfigurationVersion = scope.Configuration.VersionNumber,
            ConfigurationName = scope.Configuration.Name,
            ConfigurationHash = scope.Configuration.SettingsHash,
            IsSystemConfiguration = scope.Configuration.IsSystem
        };
    }
}
