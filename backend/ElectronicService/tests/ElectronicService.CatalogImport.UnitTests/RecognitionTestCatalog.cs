using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Core.Catalog.ProductTypes.GetAvailableCharacteristicDefinitions;
using ElectronicService.Core.Catalog.ProductTypes.GetCharacteristicSchema;
using ElectronicService.Core.Catalog.Recognition;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Effective;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Core.Catalog.Recognition.Strategies;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using ElectronicService.TestCommon;

namespace ElectronicService.CatalogImport.UnitTests;

internal sealed class RecognitionTestCatalog : ICatalogProductMetadataRepository, ICatalogProductTypeSchemaReader,
    ICatalogDictionaryReader, ICatalogCharacteristicRecognitionProfileReader,
    ICatalogRecognitionActiveRuleSetReader, ICatalogRecognitionRuleSetExecutionReader
{
    public Manufacturer Manufacturer { get; } = Manufacturer.Create("CHINT").Value;
    public CharacteristicDefinition Definition { get; } = TestDataFactory.CreateCharacteristicDefinition("CURVE", "Curve", CharacteristicDataType.Text, null);
    public ProductType ProductType { get; } = TestDataFactory.CreateProductType();
    public ProductType OtherType { get; } = TestDataFactory.CreateProductType("OTHER", "Other");
    public Dictionary<Guid, CatalogRecognitionRuleSetExecutionSnapshot> Versions { get; } = [];
    public Dictionary<Guid, Guid> ActiveVersions { get; } = [];
    public IReadOnlyCollection<CatalogDictionaryTermResult> Terms { get; set; } = [];
    public IReadOnlyCollection<CatalogCharacteristicRecognitionProfileResult> Profiles { get; set; } = [];
    public Dictionary<Guid, int> ProfileReads { get; } = [];
    public int DictionaryReads { get; private set; }
    public int Captures { get; private set; }
    public int VersionReads { get; private set; }
    public DomainError? LoadError { get; set; }

    public RecognitionTestCatalog()
    {
        TestDataFactory.AddCharacteristic(ProductType, Definition);
        TestDataFactory.AddCharacteristic(OtherType, Definition);
    }

    public CatalogEffectiveRecognitionService Service()
        => new(new CatalogProductNameRecognitionService([new TripCurveRecognitionStrategy()], this, this), this, this);

    public CatalogRecognitionRuleSetExecutionSnapshot Activate(string value = "D", ProductType? type = null)
    {
        type ??= ProductType;
        var version = new CatalogRecognitionRuleSetExecutionSnapshot(Guid.NewGuid(), Manufacturer.Id, type.Id, 1,
            [new(Guid.NewGuid(), Definition.Id, "literal-v1", "learned", value)], [], []);
        Versions.Add(version.VersionId, version);
        ActiveVersions[type.Id] = version.VersionId;
        return version;
    }

    public CatalogCharacteristicRecognitionProfileResult Profile(bool active)
        => new(Guid.NewGuid(), ProductType.Id, Definition.Id, Definition.Code, Definition.Name,
            CatalogCharacteristicRecognitionStrategyKind.EnumToken, 1000, 0.9m, "{}", active, DateTime.UtcNow, DateTime.UtcNow);

    public CatalogDictionaryTermResult Term(string value)
        => new(Guid.NewGuid(), Manufacturer.Id, ProductType.Id, "dictionary", "DICTIONARY", "Characteristic", Definition.Code,
            value, 100, "Approved", "Manual", DateTime.UtcNow, DateTime.UtcNow, null, null, null, null, null, null, null);

    public Task<IReadOnlyCollection<CatalogRecognitionRuleSetState>> CaptureForRunAsync(CancellationToken cancellationToken = default)
    {
        Captures++;
        return Task.FromResult<IReadOnlyCollection<CatalogRecognitionRuleSetState>>(ActiveVersions.Keys.Select(State).ToArray());
    }

    private CatalogRecognitionRuleSetState State(Guid typeId)
        => new(Manufacturer.Id, typeId, Captures, null, ActiveVersions.GetValueOrDefault(typeId) is var id && id != Guid.Empty ? id : null, null, null);

    public Task<Result<CatalogRecognitionRuleSetState, DomainError>> GetStateAsync(Guid manufacturerId, Guid productTypeId, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success<CatalogRecognitionRuleSetState, DomainError>(State(productTypeId)));

    public Task<Result<CatalogRecognitionActiveRuleSet, DomainError>> LoadAsync(Guid manufacturerId, Guid productTypeId, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success<CatalogRecognitionActiveRuleSet, DomainError>(new(State(productTypeId), Versions.GetValueOrDefault(ActiveVersions.GetValueOrDefault(productTypeId)))));

    public Task<Result<CatalogRecognitionRuleSetExecutionSnapshot, DomainError>> ReadAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        VersionReads++;
        return Task.FromResult(LoadError is not null
            ? Result.Failure<CatalogRecognitionRuleSetExecutionSnapshot, DomainError>(LoadError)
            : Result.Success<CatalogRecognitionRuleSetExecutionSnapshot, DomainError>(Versions[versionId]));
    }

    public Task<IReadOnlyCollection<CatalogCharacteristicRecognitionProfileResult>> GetProfilesAsync(Guid productTypeId, CancellationToken cancellationToken = default)
    {
        ProfileReads[productTypeId] = ProfileReads.GetValueOrDefault(productTypeId) + 1;
        return Task.FromResult<IReadOnlyCollection<CatalogCharacteristicRecognitionProfileResult>>(Profiles.Where(p => p.ProductTypeId == productTypeId).ToArray());
    }

    public Task<IReadOnlyCollection<CatalogDictionaryTermResult>> GetApprovedTermsAsync(CancellationToken cancellationToken = default)
    {
        DictionaryReads++;
        return Task.FromResult(Terms);
    }

    public Task<IReadOnlyCollection<CatalogDictionaryTermResult>> GetTermsAsync(CancellationToken cancellationToken = default) => Task.FromResult(Terms);
    public Task<ProductType?> GetProductTypeByIdAsync(Guid productTypeId, CancellationToken cancellationToken = default)
        => Task.FromResult(new[] { ProductType, OtherType }.FirstOrDefault(type => type.Id == productTypeId));
    public Task<CharacteristicDefinition?> GetCharacteristicDefinitionByCodeAsync(string code, CancellationToken cancellationToken = default)
        => Task.FromResult<CharacteristicDefinition?>(string.Equals(code, Definition.Code, StringComparison.Ordinal) ? Definition : null);
    public Task<Manufacturer?> GetManufacturerByIdAsync(Guid manufacturerId, CancellationToken cancellationToken = default)
        => Task.FromResult<Manufacturer?>(manufacturerId == Manufacturer.Id ? Manufacturer : null);
    public Task<IReadOnlyCollection<CharacteristicDefinition>> GetCharacteristicDefinitionsByIdsAsync(IReadOnlyCollection<Guid> characteristicDefinitionIds, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<CharacteristicDefinition>>(characteristicDefinitionIds.Contains(Definition.Id) ? [Definition] : []);
    public Task<IReadOnlyCollection<Manufacturer>> GetManufacturersAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<Manufacturer>>([Manufacturer]);

    public Task<CatalogProductTypeCharacteristicSchemaResult?> GetByCodeAsync(string productTypeCode, CancellationToken cancellationToken = default)
    {
        var type = new[] { ProductType, OtherType }.FirstOrDefault(t => string.Equals(t.Code, productTypeCode, StringComparison.Ordinal));
        return Task.FromResult(type is null ? null : new CatalogProductTypeCharacteristicSchemaResult(type.Id, type.Code, type.Name, 0,
            [new(Definition.Id, Definition.Code, Definition.Name, "Text", null, false, true, false, "Exact", 0, 0, 0)]));
    }

    public Task<IReadOnlyCollection<AvailableCharacteristicDefinitionResult>?> GetAvailableDefinitionsAsync(string productTypeCode, string? search, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<AvailableCharacteristicDefinitionResult>?>([]);
}
