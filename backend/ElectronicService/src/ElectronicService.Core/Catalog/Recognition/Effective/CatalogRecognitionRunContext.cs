using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Effective;

// One sequential recognition run. Active state is captured eagerly; each other source is read once on demand.
// This is not a transactionally consistent snapshot of all catalog tables.
public sealed class CatalogRecognitionRunContext(IReadOnlyCollection<CatalogRecognitionRuleSetState> states)
{
    public static CatalogRecognitionRunContext FromSnapshot(CatalogRecognitionRuleSetState state,
        CatalogRecognitionRuleSetExecutionSnapshot? rules, IReadOnlyCollection<CatalogDictionaryTermResult> terms,
        IReadOnlyCollection<CatalogCharacteristicRecognitionProfileResult> profiles)
    {
        var context = new CatalogRecognitionRunContext([state]) { DictionaryTerms = terms };
        context.Profiles.Add(state.ProductTypeId, profiles);
        context.ActiveRuleSets.Add((state.ManufacturerId, state.ProductTypeId), new CatalogRecognitionActiveRuleSet(state, rules));
        return context;
    }

    internal IReadOnlyDictionary<(Guid ManufacturerId, Guid ProductTypeId), CatalogRecognitionRuleSetState> ActiveStates { get; }
        = states.ToDictionary(state => (state.ManufacturerId, state.ProductTypeId));

    internal Dictionary<(Guid ManufacturerId, Guid ProductTypeId), Result<CatalogRecognitionActiveRuleSet, DomainError>> ActiveRuleSets { get; } = [];

    internal Dictionary<Guid, IReadOnlyCollection<CatalogCharacteristicRecognitionProfileResult>> Profiles { get; } = [];

    internal IReadOnlyCollection<CatalogDictionaryTermResult>? DictionaryTerms { get; set; }
}
