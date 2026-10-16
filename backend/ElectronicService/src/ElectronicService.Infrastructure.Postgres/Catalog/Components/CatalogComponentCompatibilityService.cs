using System.Globalization;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Components;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Components;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Components;

public sealed class CatalogComponentCompatibilityService
    : ICatalogComponentCompatibilityService
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogComponentCompatibilityService(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ComponentNeedResult, DomainError>> CreateNeedAsync(
        string mainProductTypeCode,
        string? code,
        string name,
        ComponentNeedRequirementMode requirementMode,
        int defaultQuantityPerUnit,
        CancellationToken cancellationToken = default)
    {
        var normalizedTypeCode = NormalizeCode(mainProductTypeCode);
        var productType = await _dbContext.ProductTypes
            .FirstOrDefaultAsync(
                item => item.Code == normalizedTypeCode,
                cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            return CatalogErrors.ProductTypeNotFound(normalizedTypeCode);
        }

        if (productType.Kind != ProductTypeKind.MainProduct)
        {
            return ComponentCompatibilityErrors.MainProductTypeRequired();
        }

        var generatedCode = string.IsNullOrWhiteSpace(name)
            ? string.Empty
            : NormalizeCode(name);
        var effectiveCode = string.IsNullOrWhiteSpace(code)
            ? generatedCode[..Math.Min(generatedCode.Length, 100)]
            : code;
        var createResult = ComponentNeedDefinition.Create(
            productType.Id,
            effectiveCode,
            name,
            requirementMode,
            defaultQuantityPerUnit);

        if (createResult.IsFailure)
        {
            return createResult.Error;
        }

        var need = createResult.Value;
        var alreadyExists = await _dbContext.ComponentNeedDefinitions
            .AnyAsync(
                item => item.MainProductTypeId == productType.Id
                    && item.Code == need.Code,
                cancellationToken)
            .ConfigureAwait(false);

        if (alreadyExists)
        {
            return ComponentCompatibilityErrors.NeedAlreadyExists(need.Code);
        }

        _dbContext.ComponentNeedDefinitions.Add(need);
        await _dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return new ComponentNeedResult(
            need.Id,
            need.MainProductTypeId,
            productType.Code,
            productType.Name,
            need.Code,
            need.Name,
            need.RequirementMode,
            need.DefaultQuantityPerUnit);
    }

    public async Task<IReadOnlyCollection<ComponentNeedResult>> GetNeedsAsync(
        string? mainProductTypeCode,
        CancellationToken cancellationToken = default)
    {
        var query =
            from need in _dbContext.ComponentNeedDefinitions.AsNoTracking()
            join productType in _dbContext.ProductTypes.AsNoTracking()
                on need.MainProductTypeId equals productType.Id
            select new { Need = need, ProductType = productType };

        if (!string.IsNullOrWhiteSpace(mainProductTypeCode))
        {
            var normalizedCode = NormalizeCode(mainProductTypeCode);
            query = query.Where(item => item.ProductType.Code == normalizedCode);
        }

        return await query
            .OrderBy(item => item.ProductType.Name)
            .ThenBy(item => item.Need.Name)
            .Select(item => new ComponentNeedResult(
                item.Need.Id,
                item.Need.MainProductTypeId,
                item.ProductType.Code,
                item.ProductType.Name,
                item.Need.Code,
                item.Need.Name,
                item.Need.RequirementMode,
                item.Need.DefaultQuantityPerUnit))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result<ComponentNeedResult, DomainError>> UpdateNeedAsync(
        Guid needDefinitionId,
        ComponentNeedRequirementMode requirementMode,
        int defaultQuantityPerUnit,
        CancellationToken cancellationToken = default)
    {
        var need = await _dbContext.ComponentNeedDefinitions
            .FirstOrDefaultAsync(
                item => item.Id == needDefinitionId,
                cancellationToken)
            .ConfigureAwait(false);

        if (need is null)
        {
            return ComponentCompatibilityErrors.NeedNotFound(needDefinitionId);
        }

        var productType = await _dbContext.ProductTypes
            .AsNoTracking()
            .FirstAsync(
                item => item.Id == need.MainProductTypeId,
                cancellationToken)
            .ConfigureAwait(false);

        var changeResult = need.ChangeSettings(
            requirementMode,
            defaultQuantityPerUnit);

        if (changeResult.IsFailure)
        {
            return changeResult.Error;
        }

        await _dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return new ComponentNeedResult(
            need.Id,
            need.MainProductTypeId,
            productType.Code,
            productType.Name,
            need.Code,
            need.Name,
            need.RequirementMode,
            need.DefaultQuantityPerUnit);
    }

    public async Task<Result<ComponentOfferResult, DomainError>> CreateOfferAsync(
        Guid componentProductId,
        Guid needDefinitionId,
        IReadOnlyCollection<ComponentCompatibilityConstraintInput> constraints,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(constraints);

        var component = await _dbContext.Products
            .FirstOrDefaultAsync(
                product => product.Id == componentProductId,
                cancellationToken)
            .ConfigureAwait(false);

        if (component is null)
        {
            return CatalogErrors.ProductNotFound(componentProductId.ToString());
        }

        var componentType = await _dbContext.ProductTypes
            .FirstAsync(
                productType => productType.Id == component.ProductTypeId,
                cancellationToken)
            .ConfigureAwait(false);

        if (componentType.Kind != ProductTypeKind.Component)
        {
            return ComponentCompatibilityErrors.ComponentProductRequired();
        }

        var need = await _dbContext.ComponentNeedDefinitions
            .FirstOrDefaultAsync(
                item => item.Id == needDefinitionId,
                cancellationToken)
            .ConfigureAwait(false);

        if (need is null)
        {
            return ComponentCompatibilityErrors.NeedNotFound(needDefinitionId);
        }

        var offerExists = await _dbContext.ComponentOffers.AnyAsync(
            offer => offer.ComponentProductId == componentProductId
                && offer.NeedDefinitionId == needDefinitionId,
            cancellationToken).ConfigureAwait(false);

        if (offerExists)
        {
            return ComponentCompatibilityErrors.OfferAlreadyExists(
                componentProductId,
                needDefinitionId);
        }

        var offerResult = ComponentOffer.Create(
            componentProductId,
            needDefinitionId);

        if (offerResult.IsFailure)
        {
            return offerResult.Error;
        }

        var offer = offerResult.Value;
        var requestedDefinitionIds = constraints
            .Select(input => input.CharacteristicDefinitionId)
            .Distinct()
            .ToArray();
        var definitions = await _dbContext.CharacteristicDefinitions
            .Where(definition => requestedDefinitionIds.Contains(definition.Id))
            .ToDictionaryAsync(definition => definition.Id, cancellationToken)
            .ConfigureAwait(false);

        var allowedDefinitionIds = await _dbContext.ProductTypeCharacteristics
            .Where(relation => relation.ProductTypeId == need.MainProductTypeId)
            .Select(relation => relation.CharacteristicDefinitionId)
            .ToHashSetAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var input in constraints)
        {
            if (!allowedDefinitionIds.Contains(input.CharacteristicDefinitionId))
            {
                return CatalogErrors.CharacteristicIsNotAllowedForProductType(
                    input.CharacteristicDefinitionId,
                    need.MainProductTypeId);
            }

            if (!definitions.TryGetValue(
                    input.CharacteristicDefinitionId,
                    out var definition))
            {
                return CatalogErrors.CharacteristicDefinitionNotFound(
                    input.CharacteristicDefinitionId);
            }

            var valueResult = ParseValue(definition.DataType, input.Value);

            if (valueResult.IsFailure)
            {
                return valueResult.Error;
            }

            var validationResult = definition.ValidateValue(valueResult.Value);

            if (validationResult.IsFailure)
            {
                return validationResult.Error;
            }

            var addResult = offer.AddConstraint(
                definition.Id,
                valueResult.Value,
                input.ComparisonOperator);

            if (addResult.IsFailure)
            {
                return addResult.Error;
            }
        }

        var rangeValidation = ValidateNumericRanges(
            offer.GetConstraints()
                .Select(item => (
                    item.CharacteristicDefinitionId,
                    item.ExpectedValue,
                    item.ComparisonOperator))
                .ToArray());
        if (rangeValidation.IsFailure)
        {
            return rangeValidation.Error;
        }

        _dbContext.ComponentOffers.Add(offer);
        await _dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return new ComponentOfferResult(
            offer.Id,
            offer.ComponentProductId,
            offer.NeedDefinitionId,
            offer.GetConstraints().Count());
    }

    public async Task<IReadOnlyCollection<ComponentOfferSummaryResult>>
        GetOffersAsync(
            Guid componentProductId,
            CancellationToken cancellationToken = default)
    {
        return await (
            from offer in _dbContext.ComponentOffers.AsNoTracking()
            join need in _dbContext.ComponentNeedDefinitions.AsNoTracking()
                on offer.NeedDefinitionId equals need.Id
            join productType in _dbContext.ProductTypes.AsNoTracking()
                on need.MainProductTypeId equals productType.Id
            where offer.ComponentProductId == componentProductId
            orderby productType.Name, need.Name
            select new ComponentOfferSummaryResult(
                offer.Id,
                need.Id,
                need.Code,
                need.Name,
                productType.Code,
                offer.ConditionGroups.SelectMany(item => item.Constraints).Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<ComponentCompatibilityRuleResult>>
        GetRulesAsync(
            string? mainProductTypeCode,
            CancellationToken cancellationToken = default)
    {
        var offersQuery = _dbContext.ComponentOffers
            .AsNoTracking()
            .Include(offer => offer.ConditionGroups)
            .ThenInclude(group => group.Constraints)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(mainProductTypeCode))
        {
            var normalizedCode = NormalizeCode(mainProductTypeCode);
            var needIds =
                from need in _dbContext.ComponentNeedDefinitions.AsNoTracking()
                join productType in _dbContext.ProductTypes.AsNoTracking()
                    on need.MainProductTypeId equals productType.Id
                where productType.Code == normalizedCode
                select need.Id;

            offersQuery = offersQuery.Where(offer =>
                needIds.Contains(offer.NeedDefinitionId));
        }

        var offers = await offersQuery
            .OrderByDescending(offer => offer.IsActive)
            .ThenBy(offer => offer.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return await MapRulesAsync(offers, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result<ComponentCompatibilityRuleResult, DomainError>>
        UpdateOfferAsync(
            Guid offerId,
            bool isActive,
            IReadOnlyCollection<ComponentCompatibilityConditionGroupInput>
                conditionGroups,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conditionGroups);

        if (conditionGroups.Count == 0)
        {
            return ComponentCompatibilityErrors.ConditionGroupsRequired();
        }

        if (conditionGroups.Count > 1
            && conditionGroups.Any(group => group.Constraints.Count == 0))
        {
            return ComponentCompatibilityErrors.UnconditionalGroupMustBeOnly();
        }

        var offer = await _dbContext.ComponentOffers
            .Include(item => item.ConditionGroups)
            .ThenInclude(group => group.Constraints)
            .FirstOrDefaultAsync(item => item.Id == offerId, cancellationToken)
            .ConfigureAwait(false);

        if (offer is null)
        {
            return ComponentCompatibilityErrors.OfferNotFound(offerId);
        }

        var need = await _dbContext.ComponentNeedDefinitions
            .AsNoTracking()
            .FirstAsync(item => item.Id == offer.NeedDefinitionId, cancellationToken)
            .ConfigureAwait(false);

        var requestedDefinitionIds = conditionGroups
            .SelectMany(group => group.Constraints)
            .Select(input => input.CharacteristicDefinitionId)
            .Distinct()
            .ToArray();
        var definitions = await _dbContext.CharacteristicDefinitions
            .AsNoTracking()
            .Where(definition => requestedDefinitionIds.Contains(definition.Id))
            .ToDictionaryAsync(definition => definition.Id, cancellationToken)
            .ConfigureAwait(false);
        var allowedDefinitionIds = await _dbContext.ProductTypeCharacteristics
            .AsNoTracking()
            .Where(relation => relation.ProductTypeId == need.MainProductTypeId)
            .Select(relation => relation.CharacteristicDefinitionId)
            .ToHashSetAsync(cancellationToken)
            .ConfigureAwait(false);

        var parsedGroups = new List<IReadOnlyCollection<(
            Guid DefinitionId,
            CharacteristicValue Value,
            ComponentCompatibilityComparisonOperator ComparisonOperator)>>();

        foreach (var groupInput in conditionGroups)
        {
            var parsedConstraints = new List<(
                Guid DefinitionId,
                CharacteristicValue Value,
                ComponentCompatibilityComparisonOperator ComparisonOperator)>();

            foreach (var input in groupInput.Constraints)
            {
                if (!allowedDefinitionIds.Contains(input.CharacteristicDefinitionId))
                {
                    return CatalogErrors.CharacteristicIsNotAllowedForProductType(
                        input.CharacteristicDefinitionId,
                        need.MainProductTypeId);
                }

                if (!definitions.TryGetValue(
                        input.CharacteristicDefinitionId,
                        out var definition))
                {
                    return CatalogErrors.CharacteristicDefinitionNotFound(
                        input.CharacteristicDefinitionId);
                }

                var valueResult = ParseValue(definition.DataType, input.Value);

                if (valueResult.IsFailure)
                {
                    return valueResult.Error;
                }

                var validationResult = definition.ValidateValue(valueResult.Value);

                if (validationResult.IsFailure)
                {
                    return validationResult.Error;
                }

                if (input.ComparisonOperator !=
                        ComponentCompatibilityComparisonOperator.Equal
                    && definition.DataType != CharacteristicDataType.Number)
                {
                    return GeneralErrors.ValueIsInvalid(
                        nameof(input.ComparisonOperator));
                }

                parsedConstraints.Add((
                    definition.Id,
                    valueResult.Value,
                    input.ComparisonOperator));
            }

            var rangeValidation = ValidateNumericRanges(parsedConstraints);
            if (rangeValidation.IsFailure)
            {
                return rangeValidation.Error;
            }

            parsedGroups.Add(parsedConstraints);
        }

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        _dbContext.ComponentCompatibilityConditionGroups.RemoveRange(
            offer.ConditionGroups);
        await _dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        offer.ClearConditionGroups();

        foreach (var parsedGroup in parsedGroups)
        {
            var groupResult = offer.AddConditionGroup();

            if (groupResult.IsFailure)
            {
                return groupResult.Error;
            }

            foreach (var parsedConstraint in parsedGroup)
            {
                var addResult = groupResult.Value.AddConstraint(
                    parsedConstraint.DefinitionId,
                    parsedConstraint.Value,
                    parsedConstraint.ComparisonOperator);

                if (addResult.IsFailure)
                {
                    return addResult.Error;
                }
            }
        }

        offer.SetActive(isActive);
        await _dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        var mapped = await MapRulesAsync(new List<ComponentOffer> { offer }, cancellationToken)
            .ConfigureAwait(false);
        return mapped.Single();
    }

    public async Task<UnitResult<DomainError>> DeleteOfferAsync(
        Guid offerId,
        CancellationToken cancellationToken = default)
    {
        var offer = await _dbContext.ComponentOffers
            .FirstOrDefaultAsync(item => item.Id == offerId, cancellationToken)
            .ConfigureAwait(false);

        if (offer is null)
        {
            return UnitResult.Failure(
                ComponentCompatibilityErrors.OfferNotFound(offerId));
        }

        _dbContext.ComponentOffers.Remove(offer);
        await _dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        return UnitResult.Success<DomainError>();
    }

    public async Task<UnitResult<DomainError>> SetNeedStatusAsync(
        Guid productId,
        Guid needDefinitionId,
        ProductNeedStatus status,
        CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(
                item => item.Id == productId,
                cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductNotFound(productId.ToString()));
        }

        var need = await _dbContext.ComponentNeedDefinitions
            .FirstOrDefaultAsync(
                item => item.Id == needDefinitionId,
                cancellationToken)
            .ConfigureAwait(false);

        if (need is null)
        {
            return UnitResult.Failure(
                ComponentCompatibilityErrors.NeedNotFound(needDefinitionId));
        }

        if (need.MainProductTypeId != product.ProductTypeId)
        {
            return UnitResult.Failure(
                ComponentCompatibilityErrors.NeedDoesNotBelongToProductType());
        }

        var existing = await _dbContext.ProductComponentNeedStates
            .FirstOrDefaultAsync(
                item => item.ProductId == productId
                    && item.NeedDefinitionId == needDefinitionId,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var createResult = ProductComponentNeedState.Create(
                productId,
                needDefinitionId,
                status);

            if (createResult.IsFailure)
            {
                return UnitResult.Failure(createResult.Error);
            }

            _dbContext.ProductComponentNeedStates.Add(createResult.Value);
        }
        else
        {
            var changeResult = existing.ChangeStatus(status);

            if (changeResult.IsFailure)
            {
                return changeResult;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        return UnitResult.Success<DomainError>();
    }

    public async Task<Result<SelectedComponentResult, DomainError>>
        SelectComponentAsync(
            Guid mainProductId,
            Guid needDefinitionId,
            Guid componentProductId,
            int quantity,
            CancellationToken cancellationToken = default)
    {
        var mainProduct = await _dbContext.Products
            .Include(product => product.Characteristics)
            .FirstOrDefaultAsync(
                product => product.Id == mainProductId,
                cancellationToken)
            .ConfigureAwait(false);

        if (mainProduct is null)
        {
            return CatalogErrors.ProductNotFound(mainProductId.ToString());
        }

        var mainProductType = await _dbContext.ProductTypes
            .AsNoTracking()
            .FirstAsync(
                productType => productType.Id == mainProduct.ProductTypeId,
                cancellationToken)
            .ConfigureAwait(false);

        if (mainProductType.Kind != ProductTypeKind.MainProduct)
        {
            return ComponentCompatibilityErrors.MainProductRequired();
        }

        var need = await _dbContext.ComponentNeedDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == needDefinitionId,
                cancellationToken)
            .ConfigureAwait(false);

        if (need is null)
        {
            return ComponentCompatibilityErrors.NeedNotFound(needDefinitionId);
        }

        if (need.MainProductTypeId != mainProduct.ProductTypeId)
        {
            return ComponentCompatibilityErrors.NeedDoesNotBelongToProductType();
        }

        var component = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                product => product.Id == componentProductId,
                cancellationToken)
            .ConfigureAwait(false);

        if (component is null)
        {
            return CatalogErrors.ProductNotFound(componentProductId.ToString());
        }

        var componentType = await _dbContext.ProductTypes
            .AsNoTracking()
            .FirstAsync(
                productType => productType.Id == component.ProductTypeId,
                cancellationToken)
            .ConfigureAwait(false);

        if (componentType.Kind != ProductTypeKind.Component)
        {
            return ComponentCompatibilityErrors.ComponentProductRequired();
        }

        var offer = await _dbContext.ComponentOffers
            .AsNoTracking()
            .Include(item => item.ConditionGroups)
            .ThenInclude(group => group.Constraints)
            .FirstOrDefaultAsync(
                item => item.ComponentProductId == componentProductId
                    && item.NeedDefinitionId == needDefinitionId
                    && item.IsActive,
                cancellationToken)
            .ConfigureAwait(false);

        var mainProductValues = mainProduct.Characteristics.ToDictionary(
            characteristic => characteristic.CharacteristicDefinitionId,
            characteristic => characteristic.Value);

        if (offer is null
            || !ComponentCompatibilityMatcher.IsCompatible(
                offer.ConditionGroups,
                mainProductValues))
        {
            return ComponentCompatibilityErrors.CompatibleOfferNotFound();
        }

        var alreadyExists = await _dbContext.ProductSelectedComponents
            .AnyAsync(
                selection => selection.MainProductId == mainProductId
                    && selection.NeedDefinitionId == needDefinitionId
                    && selection.ComponentProductId == componentProductId,
                cancellationToken)
            .ConfigureAwait(false);

        if (alreadyExists)
        {
            return ComponentCompatibilityErrors.SelectedComponentAlreadyExists();
        }

        var createResult = ProductSelectedComponent.Create(
            mainProductId,
            needDefinitionId,
            componentProductId,
            quantity);

        if (createResult.IsFailure)
        {
            return createResult.Error;
        }

        var selection = createResult.Value;
        _dbContext.ProductSelectedComponents.Add(selection);
        await _dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return MapSelectedComponent(selection, component);
    }

    public async Task<Result<SelectedComponentResult, DomainError>>
        ChangeSelectedQuantityAsync(
            Guid mainProductId,
            Guid selectionId,
            int quantity,
            CancellationToken cancellationToken = default)
    {
        var selection = await _dbContext.ProductSelectedComponents
            .FirstOrDefaultAsync(
                item => item.Id == selectionId
                    && item.MainProductId == mainProductId,
                cancellationToken)
            .ConfigureAwait(false);

        if (selection is null)
        {
            return ComponentCompatibilityErrors.SelectedComponentNotFound(
                selectionId);
        }

        var changeResult = selection.ChangeQuantity(quantity);

        if (changeResult.IsFailure)
        {
            return changeResult.Error;
        }

        var component = await _dbContext.Products
            .AsNoTracking()
            .FirstAsync(
                product => product.Id == selection.ComponentProductId,
                cancellationToken)
            .ConfigureAwait(false);

        await _dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return MapSelectedComponent(selection, component);
    }

    public async Task<UnitResult<DomainError>> RemoveSelectedComponentAsync(
        Guid mainProductId,
        Guid selectionId,
        CancellationToken cancellationToken = default)
    {
        var selection = await _dbContext.ProductSelectedComponents
            .FirstOrDefaultAsync(
                item => item.Id == selectionId
                    && item.MainProductId == mainProductId,
                cancellationToken)
            .ConfigureAwait(false);

        if (selection is null)
        {
            return UnitResult.Failure(
                ComponentCompatibilityErrors.SelectedComponentNotFound(
                    selectionId));
        }

        _dbContext.ProductSelectedComponents.Remove(selection);
        await _dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        return UnitResult.Success<DomainError>();
    }

    public async Task<Result<ProductComponentCompatibilityResult, DomainError>>
        GetCompatibilityAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(item => item.Characteristics)
            .FirstOrDefaultAsync(
                item => item.Id == productId,
                cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return CatalogErrors.ProductNotFound(productId.ToString());
        }

        var needs = await _dbContext.ComponentNeedDefinitions
            .AsNoTracking()
            .Where(need => need.MainProductTypeId == product.ProductTypeId)
            .OrderBy(need => need.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var needIds = needs.Select(need => need.Id).ToArray();
        var states = await _dbContext.ProductComponentNeedStates
            .AsNoTracking()
            .Where(state => state.ProductId == productId
                && needIds.Contains(state.NeedDefinitionId))
            .ToDictionaryAsync(
                state => state.NeedDefinitionId,
                state => state.Status,
                cancellationToken)
            .ConfigureAwait(false);

        var offers = await _dbContext.ComponentOffers
            .AsNoTracking()
            .Include(offer => offer.ConditionGroups)
            .ThenInclude(group => group.Constraints)
            .Where(offer => offer.IsActive
                && needIds.Contains(offer.NeedDefinitionId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var componentIds = offers
            .Select(offer => offer.ComponentProductId)
            .Distinct()
            .ToArray();
        var components = await _dbContext.Products
            .AsNoTracking()
            .Where(component => componentIds.Contains(component.Id))
            .ToDictionaryAsync(component => component.Id, cancellationToken)
            .ConfigureAwait(false);

        var characteristicValues = product.Characteristics.ToDictionary(
            characteristic => characteristic.CharacteristicDefinitionId,
            characteristic => characteristic.Value);
        var constrainedDefinitionIds = offers
            .SelectMany(offer => offer.GetConstraints())
            .Select(constraint => constraint.CharacteristicDefinitionId)
            .Distinct()
            .ToArray();
        var characteristicNames = constrainedDefinitionIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.CharacteristicDefinitions
                .AsNoTracking()
                .Where(definition => constrainedDefinitionIds.Contains(definition.Id))
                .ToDictionaryAsync(
                    definition => definition.Id,
                    definition => definition.Name,
                    cancellationToken)
                .ConfigureAwait(false);

        var selectedComponents = await (
            from selection in _dbContext.ProductSelectedComponents.AsNoTracking()
            join component in _dbContext.Products.AsNoTracking()
                on selection.ComponentProductId equals component.Id
            where selection.MainProductId == productId
                && needIds.Contains(selection.NeedDefinitionId)
            select new SelectedComponentResult(
                selection.Id,
                selection.NeedDefinitionId,
                component.Id,
                component.Article.Value,
                component.Name.Value,
                selection.Quantity,
                component.Price.Amount,
                component.Price.Currency,
                component.Price.Amount * selection.Quantity))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var results = needs.Select(need =>
        {
            var compatibleComponents = offers
                .Where(offer => offer.NeedDefinitionId == need.Id)
                .Where(offer => ComponentCompatibilityMatcher.IsCompatible(
                    offer.ConditionGroups,
                    characteristicValues))
                .Select(offer => new
                {
                    Component = components.GetValueOrDefault(offer.ComponentProductId),
                    MatchingGroups = ComponentCompatibilityMatcher.GetMatchingGroups(
                        offer.ConditionGroups,
                        characteristicValues)
                })
                .Select(item => new
                {
                    item.Component,
                    MatchConditions = item.MatchingGroups
                        .SelectMany(group => group.Constraints)
                        .GroupBy(constraint => constraint.CharacteristicDefinitionId)
                        .Select(group => new ComponentCompatibilityConditionResult(
                            characteristicNames.GetValueOrDefault(
                                group.Key,
                                "Характеристика"),
                            characteristicValues[group.Key].ToString(),
                            group.Select(FormatConstraintValue)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToArray()))
                        .OrderBy(condition => condition.CharacteristicName,
                            StringComparer.OrdinalIgnoreCase)
                        .ToArray()
                })
                .Where(item => item.Component is not null)
                .Select(item => new CompatibleComponentResult(
                    item.Component!.Id,
                    item.Component.Article.Value,
                    item.Component.Name.Value,
                    item.Component.Price.Amount,
                    item.Component.Price.Currency,
                    item.MatchConditions))
                .OrderBy(component => component.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new ProductNeedCompatibilityResult(
                need.Id,
                need.Code,
                need.Name,
                need.RequirementMode,
                need.DefaultQuantityPerUnit,
                states.GetValueOrDefault(need.Id, ProductNeedStatus.Unknown),
                compatibleComponents,
                selectedComponents
                    .Where(selection => selection.NeedDefinitionId == need.Id)
                    .OrderBy(selection => selection.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList());
        }).ToList();

        return new ProductComponentCompatibilityResult(product.Id, results);
    }

    public async Task<UnitResult<DomainError>> ValidateManualSelectionAsync(
        Guid mainProductId,
        Guid needDefinitionId,
        Guid componentProductId,
        CancellationToken cancellationToken = default)
    {
        var mainProduct = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                product => product.Id == mainProductId,
                cancellationToken)
            .ConfigureAwait(false);

        if (mainProduct is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductNotFound(mainProductId.ToString()));
        }

        var mainProductType = await _dbContext.ProductTypes
            .AsNoTracking()
            .FirstAsync(
                productType => productType.Id == mainProduct.ProductTypeId,
                cancellationToken)
            .ConfigureAwait(false);

        if (mainProductType.Kind != ProductTypeKind.MainProduct)
        {
            return UnitResult.Failure(
                ComponentCompatibilityErrors.MainProductRequired());
        }

        var need = await _dbContext.ComponentNeedDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == needDefinitionId,
                cancellationToken)
            .ConfigureAwait(false);

        if (need is null)
        {
            return UnitResult.Failure(
                ComponentCompatibilityErrors.NeedNotFound(needDefinitionId));
        }

        if (need.MainProductTypeId != mainProduct.ProductTypeId)
        {
            return UnitResult.Failure(
                ComponentCompatibilityErrors.NeedDoesNotBelongToProductType());
        }

        var component = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                product => product.Id == componentProductId,
                cancellationToken)
            .ConfigureAwait(false);

        if (component is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductNotFound(componentProductId.ToString()));
        }

        var componentType = await _dbContext.ProductTypes
            .AsNoTracking()
            .FirstAsync(
                productType => productType.Id == component.ProductTypeId,
                cancellationToken)
            .ConfigureAwait(false);

        return componentType.Kind == ProductTypeKind.Component
            ? UnitResult.Success<DomainError>()
            : UnitResult.Failure(
                ComponentCompatibilityErrors.ComponentProductRequired());
    }

    private async Task<IReadOnlyCollection<ComponentCompatibilityRuleResult>>
        MapRulesAsync(
            List<ComponentOffer> offers,
            CancellationToken cancellationToken)
    {
        if (offers.Count == 0)
        {
            return [];
        }

        var componentIds = offers
            .Select(offer => offer.ComponentProductId)
            .Distinct()
            .ToArray();
        var needIds = offers
            .Select(offer => offer.NeedDefinitionId)
            .Distinct()
            .ToArray();
        var definitionIds = offers
            .SelectMany(offer => offer.GetConstraints())
            .Select(constraint => constraint.CharacteristicDefinitionId)
            .Distinct()
            .ToArray();

        var components = await _dbContext.Products
            .AsNoTracking()
            .Where(product => componentIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken)
            .ConfigureAwait(false);
        var needs = await _dbContext.ComponentNeedDefinitions
            .AsNoTracking()
            .Where(need => needIds.Contains(need.Id))
            .ToDictionaryAsync(need => need.Id, cancellationToken)
            .ConfigureAwait(false);
        var typeIds = needs.Values
            .Select(need => need.MainProductTypeId)
            .Distinct()
            .ToArray();
        var productTypes = await _dbContext.ProductTypes
            .AsNoTracking()
            .Where(productType => typeIds.Contains(productType.Id))
            .ToDictionaryAsync(productType => productType.Id, cancellationToken)
            .ConfigureAwait(false);
        var definitions = await _dbContext.CharacteristicDefinitions
            .AsNoTracking()
            .Where(definition => definitionIds.Contains(definition.Id))
            .ToDictionaryAsync(definition => definition.Id, cancellationToken)
            .ConfigureAwait(false);

        return offers
            .Where(offer => components.ContainsKey(offer.ComponentProductId)
                && needs.ContainsKey(offer.NeedDefinitionId))
            .Select(offer =>
            {
                var component = components[offer.ComponentProductId];
                var need = needs[offer.NeedDefinitionId];
                var productType = productTypes[need.MainProductTypeId];

                return new ComponentCompatibilityRuleResult(
                    offer.Id,
                    component.Id,
                    component.Article.Value,
                    component.Name.Value,
                    need.Id,
                    need.Code,
                    need.Name,
                    productType.Code,
                    productType.Name,
                    offer.IsActive,
                    offer.ConditionGroups
                        .OrderBy(group => group.Position)
                        .Select(group =>
                            new ComponentCompatibilityRuleGroupResult(
                                group.Id,
                                group.Position,
                                group.Constraints
                                    .OrderBy(
                                        constraint => definitions
                                            .GetValueOrDefault(
                                                constraint.CharacteristicDefinitionId)
                                            ?.Name,
                                        StringComparer.OrdinalIgnoreCase)
                                    .Select(constraint =>
                                        new ComponentCompatibilityRuleConstraintResult(
                                            constraint.CharacteristicDefinitionId,
                                            definitions.GetValueOrDefault(
                                                    constraint.CharacteristicDefinitionId)
                                                ?.Name
                                                ?? "Характеристика",
                                            constraint.ExpectedValue.ToString(),
                                            constraint.ComparisonOperator))
                                    .ToList()))
                        .ToList());
            })
            .OrderBy(rule => rule.MainProductTypeName,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(rule => rule.NeedName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(rule => rule.ComponentName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static SelectedComponentResult MapSelectedComponent(
        ProductSelectedComponent selection,
        Domain.Catalog.Products.Product component)
    {
        return new SelectedComponentResult(
            selection.Id,
            selection.NeedDefinitionId,
            selection.ComponentProductId,
            component.Article.Value,
            component.Name.Value,
            selection.Quantity,
            component.Price.Amount,
            component.Price.Currency,
            component.Price.Amount * selection.Quantity);
    }

    private static Result<CharacteristicValue, DomainError> ParseValue(
        CharacteristicDataType dataType,
        string rawValue)
    {
        return dataType switch
        {
            CharacteristicDataType.Text => CharacteristicValue.CreateText(rawValue),
            CharacteristicDataType.Number when decimal.TryParse(
                rawValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var number) => CharacteristicValue.CreateNumber(number),
            CharacteristicDataType.Boolean when bool.TryParse(
                rawValue,
                out var boolean) => CharacteristicValue.CreateBoolean(boolean),
            _ => Result.Failure<CharacteristicValue, DomainError>(
                GeneralErrors.ValueIsInvalid(nameof(rawValue)))
        };
    }

    private static string FormatConstraintValue(
        ComponentCompatibilityConstraint constraint)
    {
        var prefix = constraint.ComparisonOperator switch
        {
            ComponentCompatibilityComparisonOperator.GreaterThanOrEqual => "от ",
            ComponentCompatibilityComparisonOperator.LessThanOrEqual => "до ",
            _ => string.Empty
        };

        return prefix + constraint.ExpectedValue;
    }

    private static UnitResult<DomainError> ValidateNumericRanges(
        IReadOnlyCollection<(
            Guid DefinitionId,
            CharacteristicValue Value,
            ComponentCompatibilityComparisonOperator ComparisonOperator)> constraints)
    {
        foreach (var group in constraints.GroupBy(item => item.DefinitionId))
        {
            var equalValues = group
                .Where(item => item.ComparisonOperator ==
                    ComponentCompatibilityComparisonOperator.Equal)
                .ToArray();
            var lowerBounds = group
                .Where(item => item.ComparisonOperator ==
                    ComponentCompatibilityComparisonOperator.GreaterThanOrEqual)
                .Select(item => item.Value.NumberValue)
                .Where(item => item.HasValue)
                .Select(item => item!.Value)
                .ToArray();
            var upperBounds = group
                .Where(item => item.ComparisonOperator ==
                    ComponentCompatibilityComparisonOperator.LessThanOrEqual)
                .Select(item => item.Value.NumberValue)
                .Where(item => item.HasValue)
                .Select(item => item!.Value)
                .ToArray();

            if (lowerBounds.Length > 1 || upperBounds.Length > 1)
            {
                return UnitResult.Failure(
                    GeneralErrors.ValueIsInvalid("Диапазон характеристики"));
            }

            if (equalValues.Length > 0
                && (lowerBounds.Length > 0 || upperBounds.Length > 0))
            {
                return UnitResult.Failure(
                    GeneralErrors.ValueIsInvalid("Диапазон характеристики"));
            }

            if (lowerBounds.Length == 1
                && upperBounds.Length == 1
                && lowerBounds[0] > upperBounds[0])
            {
                return UnitResult.Failure(
                    GeneralErrors.ValueIsInvalid("Диапазон характеристики"));
            }
        }

        return UnitResult.Success<DomainError>();
    }

    private static string NormalizeCode(string value)
    {
        return value.Trim().ToUpperInvariant()
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal);
    }
}
