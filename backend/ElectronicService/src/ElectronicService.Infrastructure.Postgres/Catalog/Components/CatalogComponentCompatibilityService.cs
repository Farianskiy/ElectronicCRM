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
        string code,
        string name,
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

        var createResult = ComponentNeedDefinition.Create(
            productType.Id,
            code,
            name);

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
            need.Name);
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
                item.Need.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
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
                valueResult.Value);

            if (addResult.IsFailure)
            {
                return addResult.Error;
            }
        }

        _dbContext.ComponentOffers.Add(offer);
        await _dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return new ComponentOfferResult(
            offer.Id,
            offer.ComponentProductId,
            offer.NeedDefinitionId,
            offer.Constraints.Count);
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
                offer.Constraints.Count))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
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
            .Include(item => item.Constraints)
            .FirstOrDefaultAsync(
                item => item.ComponentProductId == componentProductId
                    && item.NeedDefinitionId == needDefinitionId,
                cancellationToken)
            .ConfigureAwait(false);

        var mainProductValues = mainProduct.Characteristics.ToDictionary(
            characteristic => characteristic.CharacteristicDefinitionId,
            characteristic => characteristic.Value);

        if (offer is null
            || !ComponentCompatibilityMatcher.IsCompatible(
                offer.Constraints,
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
            .Include(offer => offer.Constraints)
            .Where(offer => needIds.Contains(offer.NeedDefinitionId))
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
                    offer.Constraints,
                    characteristicValues))
                .Select(offer => components.GetValueOrDefault(
                    offer.ComponentProductId))
                .Where(component => component is not null)
                .Select(component => new CompatibleComponentResult(
                    component!.Id,
                    component.Article.Value,
                    component.Name.Value,
                    component.Price.Amount,
                    component.Price.Currency))
                .OrderBy(component => component.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new ProductNeedCompatibilityResult(
                need.Id,
                need.Code,
                need.Name,
                states.GetValueOrDefault(need.Id, ProductNeedStatus.Unknown),
                compatibleComponents,
                selectedComponents
                    .Where(selection => selection.NeedDefinitionId == need.Id)
                    .OrderBy(selection => selection.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList());
        }).ToList();

        return new ProductComponentCompatibilityResult(product.Id, results);
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

    private static string NormalizeCode(string value)
    {
        return value.Trim().ToUpperInvariant()
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal);
    }
}
