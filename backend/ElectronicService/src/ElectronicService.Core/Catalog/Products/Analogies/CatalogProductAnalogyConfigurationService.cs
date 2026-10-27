using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.Analogies;

public sealed record CatalogProductAnalogyConfigurationView(
    Guid? Id,
    int? VersionNumber,
    string Name,
    string Status,
    string SettingsHash,
    bool IsSystem,
    CatalogProductAnalogySettings Settings,
    DateTime? CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    uint? Version);

public sealed record CatalogProductAnalogyConfigurationWorkspace(
    CatalogProductAnalogyConfigurationView Active,
    IReadOnlyCollection<CatalogProductAnalogyConfigurationView> Versions);

public sealed class CatalogProductAnalogyConfigurationResolver
{
    private static readonly (CatalogProductAnalogySettings Settings, string Json, string Hash)
        SystemConfiguration = CatalogProductAnalogySettingsCodec
            .Normalize(CatalogProductAnalogySettings.SystemDefault)
            .Value;

    private readonly ICatalogProductAnalogyConfigurationRepository _repository;

    public CatalogProductAnalogyConfigurationResolver(
        ICatalogProductAnalogyConfigurationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CatalogProductAnalogyConfigurationSnapshot, DomainError>>
        ResolveAsync(
            Guid manufacturerId,
            Guid productTypeId,
            CancellationToken cancellationToken = default)
    {
        var active = await _repository
            .GetActiveAsync(manufacturerId, productTypeId, cancellationToken)
            .ConfigureAwait(false);
        if (active is null)
        {
            return new CatalogProductAnalogyConfigurationSnapshot(
                null,
                null,
                "Системная конфигурация",
                SystemConfiguration.Hash,
                true,
                SystemConfiguration.Settings);
        }

        var settings = CatalogProductAnalogySettingsCodec.Deserialize(
            active.SettingsJson);
        return settings.IsFailure
            ? settings.Error
            : new CatalogProductAnalogyConfigurationSnapshot(
                active.Id,
                active.VersionNumber,
                active.Name,
                active.SettingsHash,
                false,
                settings.Value);
    }
}

public sealed class CatalogProductAnalogyConfigurationService
{
    private readonly ICatalogProductAnalogyConfigurationRepository _repository;
    private readonly ICatalogProductMetadataRepository _metadataRepository;
    private readonly CatalogProductAnalogyConfigurationResolver _resolver;

    public CatalogProductAnalogyConfigurationService(
        ICatalogProductAnalogyConfigurationRepository repository,
        ICatalogProductMetadataRepository metadataRepository,
        CatalogProductAnalogyConfigurationResolver resolver)
    {
        _repository = repository;
        _metadataRepository = metadataRepository;
        _resolver = resolver;
    }

    public async Task<Result<CatalogProductAnalogyConfigurationWorkspace, DomainError>>
        GetWorkspaceAsync(
            Guid manufacturerId,
            Guid productTypeId,
            CancellationToken cancellationToken = default)
    {
        var scope = await ValidateScopeAsync(
            manufacturerId,
            productTypeId,
            cancellationToken).ConfigureAwait(false);
        if (scope.IsFailure)
        {
            return scope.Error;
        }

        var active = await _resolver.ResolveAsync(
            manufacturerId,
            productTypeId,
            cancellationToken).ConfigureAwait(false);
        if (active.IsFailure)
        {
            return active.Error;
        }

        var versions = await _repository.ListAsync(
            manufacturerId,
            productTypeId,
            cancellationToken).ConfigureAwait(false);
        var mapped = new List<CatalogProductAnalogyConfigurationView>();
        foreach (var version in versions)
        {
            var settings = CatalogProductAnalogySettingsCodec.Deserialize(
                version.SettingsJson);
            if (settings.IsFailure)
            {
                return settings.Error;
            }

            mapped.Add(Map(version, settings.Value));
        }

        var activeView = new CatalogProductAnalogyConfigurationView(
            active.Value.ConfigurationId,
            active.Value.VersionNumber,
            active.Value.Name,
            active.Value.IsSystem ? "System" : "Active",
            active.Value.SettingsHash,
            active.Value.IsSystem,
            active.Value.Settings,
            null,
            null,
            null);
        return new CatalogProductAnalogyConfigurationWorkspace(
            activeView,
            mapped);
    }

    public async Task<Result<CatalogProductAnalogyConfigurationView, DomainError>>
        CreateDraftAsync(
            Guid manufacturerId,
            Guid productTypeId,
            string name,
            CatalogProductAnalogySettings settings,
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        var allowedCodes = await ValidateScopeAsync(
            manufacturerId,
            productTypeId,
            cancellationToken).ConfigureAwait(false);
        if (allowedCodes.IsFailure)
        {
            return allowedCodes.Error;
        }

        var normalized = ValidateSettings(settings, allowedCodes.Value);
        if (normalized.IsFailure)
        {
            return normalized.Error;
        }

        var next = await _repository.GetNextVersionNumberAsync(
            manufacturerId,
            productTypeId,
            cancellationToken).ConfigureAwait(false);
        var created = CatalogProductAnalogyConfiguration.CreateDraft(
            manufacturerId,
            productTypeId,
            next,
            name,
            normalized.Value.Json,
            normalized.Value.Hash,
            userId);
        if (created.IsFailure)
        {
            return created.Error;
        }

        _repository.Add(created.Value);
        if (!await _repository.TrySaveChangesAsync(cancellationToken)
                .ConfigureAwait(false))
        {
            return new DomainError(
                "catalog_analogy.concurrency",
                "Конфигурация была изменена одновременно. Обновите список.");
        }

        return Map(created.Value, normalized.Value.Settings);
    }

    public async Task<Result<CatalogProductAnalogyConfigurationView, DomainError>>
        UpdateDraftAsync(
            Guid id,
            string name,
            CatalogProductAnalogySettings settings,
            uint expectedVersion,
            CancellationToken cancellationToken = default)
    {
        var configuration = await _repository.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (configuration is null)
        {
            return new DomainError(
                "catalog_analogy.not_found",
                "Черновик конфигурации не найден.");
        }

        if (configuration.Version != expectedVersion)
        {
            return new DomainError(
                "catalog_analogy.concurrency",
                "Черновик уже изменён. Обновите страницу.");
        }

        var allowedCodes = await ValidateScopeAsync(
            configuration.ManufacturerId,
            configuration.ProductTypeId,
            cancellationToken).ConfigureAwait(false);
        if (allowedCodes.IsFailure)
        {
            return allowedCodes.Error;
        }

        var normalized = ValidateSettings(settings, allowedCodes.Value);
        if (normalized.IsFailure)
        {
            return normalized.Error;
        }

        var update = configuration.UpdateDraft(
            name,
            normalized.Value.Json,
            normalized.Value.Hash);
        if (update.IsFailure)
        {
            return update.Error;
        }

        if (!await _repository.TrySaveChangesAsync(cancellationToken)
                .ConfigureAwait(false))
        {
            return new DomainError(
                "catalog_analogy.concurrency",
                "Черновик уже изменён. Обновите страницу.");
        }

        return Map(configuration, normalized.Value.Settings);
    }

    public async Task<Result<CatalogProductAnalogyConfigurationView, DomainError>>
        CreateFromVersionAsync(
            Guid sourceId,
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        var source = await _repository.GetByIdAsync(sourceId, cancellationToken)
            .ConfigureAwait(false);
        if (source is null)
        {
            return new DomainError("catalog_analogy.not_found", "Исходная версия не найдена.");
        }

        var settings = CatalogProductAnalogySettingsCodec.Deserialize(source.SettingsJson);
        if (settings.IsFailure) return settings.Error;
        return await CreateDraftAsync(source.ManufacturerId, source.ProductTypeId,
            $"На основе версии {source.VersionNumber}: {source.Name}", settings.Value,
            userId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<IReadOnlySet<string>, DomainError>> ValidateScopeAsync(
        Guid manufacturerId,
        Guid productTypeId,
        CancellationToken cancellationToken)
    {
        if (manufacturerId == Guid.Empty || productTypeId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(manufacturerId));
        }

        var manufacturer = await _metadataRepository.GetManufacturerByIdAsync(
            manufacturerId,
            cancellationToken).ConfigureAwait(false);
        var productType = await _metadataRepository.GetProductTypeByIdAsync(
            productTypeId,
            cancellationToken).ConfigureAwait(false);
        if (manufacturer is null || productType is null)
        {
            return new DomainError(
                "catalog_analogy.scope_not_found",
                "Производитель или тип товара не найден.");
        }

        var ids = productType.Characteristics
            .Select(item => item.CharacteristicDefinitionId)
            .Distinct()
            .ToArray();
        var definitions = await _metadataRepository
            .GetCharacteristicDefinitionsByIdsAsync(ids, cancellationToken)
            .ConfigureAwait(false);
        return definitions
            .Select(item => CatalogProductAnalogySettings.NormalizeCode(item.Code))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static Result<(CatalogProductAnalogySettings Settings, string Json, string Hash), DomainError>
        ValidateSettings(
            CatalogProductAnalogySettings settings,
            IReadOnlySet<string> allowedCodes)
    {
        var normalized = CatalogProductAnalogySettingsCodec.Normalize(settings);
        if (normalized.IsFailure)
        {
            return normalized.Error;
        }

        if (normalized.Value.Settings.Characteristics.Keys.Any(code =>
                !allowedCodes.Contains(code)))
        {
            return new DomainError(
                "catalog_analogy.invalid_characteristic",
                "Конфигурация содержит характеристику другого типа товара.");
        }

        return normalized.Value;
    }

    private static CatalogProductAnalogyConfigurationView Map(
        CatalogProductAnalogyConfiguration configuration,
        CatalogProductAnalogySettings settings) =>
        new(
            configuration.Id,
            configuration.VersionNumber,
            configuration.Name,
            configuration.Status.ToString(),
            configuration.SettingsHash,
            false,
            settings,
            configuration.CreatedAtUtc,
            configuration.UpdatedAtUtc,
            configuration.Version);
}
