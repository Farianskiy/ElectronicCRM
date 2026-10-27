using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Products;

public sealed class CatalogProductAnalogyConfiguration : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int SettingsJsonMaxLength = 32000;
    public const int SettingsHashMaxLength = 64;

    private CatalogProductAnalogyConfiguration()
    {
    }

    private CatalogProductAnalogyConfiguration(Guid id) : base(id)
    {
    }

    public Guid ManufacturerId { get; private set; }
    public Guid ProductTypeId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public CatalogProductAnalogyConfigurationStatus Status { get; private set; }
    public string SettingsJson { get; private set; } = string.Empty;
    public string SettingsHash { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? EvaluatedAtUtc { get; private set; }
    public DateTime? ActivatedAtUtc { get; private set; }
    public DateTime? SupersededAtUtc { get; private set; }
    public uint Version { get; private set; }

    public static Result<CatalogProductAnalogyConfiguration, DomainError> CreateDraft(
        Guid manufacturerId,
        Guid productTypeId,
        int versionNumber,
        string name,
        string settingsJson,
        string settingsHash,
        Guid createdByUserId)
    {
        var validation = Validate(
            manufacturerId,
            productTypeId,
            versionNumber,
            name,
            settingsJson,
            settingsHash,
            createdByUserId);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        var now = DateTime.UtcNow;
        return new CatalogProductAnalogyConfiguration(Guid.CreateVersion7())
        {
            ManufacturerId = manufacturerId,
            ProductTypeId = productTypeId,
            VersionNumber = versionNumber,
            Name = name.Trim(),
            Status = CatalogProductAnalogyConfigurationStatus.Draft,
            SettingsJson = settingsJson.Trim(),
            SettingsHash = settingsHash.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public UnitResult<DomainError> UpdateDraft(
        string name,
        string settingsJson,
        string settingsHash)
    {
        if (Status != CatalogProductAnalogyConfigurationStatus.Draft)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(Status)));
        }

        var validation = ValidateText(name, settingsJson, settingsHash);
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = name.Trim();
        SettingsJson = settingsJson.Trim();
        SettingsHash = settingsHash.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
        EvaluatedAtUtc = null;
        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> MarkEvaluated()
    {
        if (Status is not (CatalogProductAnalogyConfigurationStatus.Draft
            or CatalogProductAnalogyConfigurationStatus.Evaluated))
        {
            return UnitResult.Failure(new DomainError(
                "catalog_analogy.invalid_status",
                "Оценивать можно только черновик или ранее оценённую версию."));
        }

        Status = CatalogProductAnalogyConfigurationStatus.Evaluated;
        EvaluatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = EvaluatedAtUtc.Value;
        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Activate()
    {
        if (Status != CatalogProductAnalogyConfigurationStatus.Evaluated)
        {
            return UnitResult.Failure(new DomainError(
                "catalog_analogy.invalid_status",
                "Включить можно только оценённую версию."));
        }

        Status = CatalogProductAnalogyConfigurationStatus.Active;
        ActivatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = ActivatedAtUtc.Value;
        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Supersede()
    {
        if (Status != CatalogProductAnalogyConfigurationStatus.Active)
        {
            return UnitResult.Failure(new DomainError(
                "catalog_analogy.invalid_status",
                "Заменить можно только действующую версию."));
        }

        Status = CatalogProductAnalogyConfigurationStatus.Superseded;
        SupersededAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = SupersededAtUtc.Value;
        return UnitResult.Success<DomainError>();
    }

    private static UnitResult<DomainError> Validate(
        Guid manufacturerId,
        Guid productTypeId,
        int versionNumber,
        string name,
        string settingsJson,
        string settingsHash,
        Guid createdByUserId)
    {
        if (manufacturerId == Guid.Empty || productTypeId == Guid.Empty
            || createdByUserId == Guid.Empty || versionNumber < 1)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(manufacturerId)));
        }

        return ValidateText(name, settingsJson, settingsHash);
    }

    private static UnitResult<DomainError> ValidateText(
        string name,
        string settingsJson,
        string settingsHash)
    {
        if (string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(settingsJson)
            || string.IsNullOrWhiteSpace(settingsHash))
        {
            return UnitResult.Failure(GeneralErrors.ValueIsRequired(nameof(name)));
        }

        if (name.Trim().Length > NameMaxLength
            || settingsJson.Trim().Length > SettingsJsonMaxLength
            || settingsHash.Trim().Length > SettingsHashMaxLength)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsTooLong(
                nameof(name), NameMaxLength));
        }

        return UnitResult.Success<DomainError>();
    }
}
