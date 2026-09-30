using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.PriceCalculations;

public sealed class CatalogPriceCalculationLineComponent
    : Abstractions.Entity
{
    public const int MaximumNeedNameLength = 200;

    public const int MaximumQuantityPerUnit = 1_000_000;

    private CatalogPriceCalculationLineComponent(
        Guid id,
        Guid calculationLineId,
        Guid needDefinitionId,
        string needName,
        Guid componentProductId,
        Guid manufacturerId,
        string article,
        string name,
        string manufacturerName,
        int quantityPerUnit,
        decimal basePriceAmount,
        decimal discountPercent,
        decimal parentQuantity)
        : base(id)
    {
        CalculationLineId = calculationLineId;
        NeedDefinitionId = needDefinitionId;
        NeedName = needName;
        ComponentProductId = componentProductId;
        ManufacturerId = manufacturerId;
        Article = article;
        Name = name;
        ManufacturerName = manufacturerName;
        QuantityPerUnit = quantityPerUnit;
        BasePriceAmount = basePriceAmount;
        DiscountPercent = discountPercent;
        CreatedAtUtc = DateTime.UtcNow;

        Recalculate(parentQuantity);
    }

    private CatalogPriceCalculationLineComponent()
    {
    }

    public Guid CalculationLineId
    {
        get;
        private set;
    }

    public Guid NeedDefinitionId
    {
        get;
        private set;
    }

    public string NeedName
    {
        get;
        private set;
    } = string.Empty;

    public Guid ComponentProductId
    {
        get;
        private set;
    }

    public Guid ManufacturerId
    {
        get;
        private set;
    }

    public string Article
    {
        get;
        private set;
    } = string.Empty;

    public string Name
    {
        get;
        private set;
    } = string.Empty;

    public string ManufacturerName
    {
        get;
        private set;
    } = string.Empty;

    public int QuantityPerUnit
    {
        get;
        private set;
    }

    public decimal BasePriceAmount
    {
        get;
        private set;
    }

    public decimal DiscountPercent
    {
        get;
        private set;
    }

    public decimal ProjectPriceAmount
    {
        get;
        private set;
    }

    public decimal TotalQuantity
    {
        get;
        private set;
    }

    public decimal TotalAmount
    {
        get;
        private set;
    }

    public DateTime CreatedAtUtc
    {
        get;
        private set;
    }

    public DateTime? UpdatedAtUtc
    {
        get;
        private set;
    }

    internal static Result<
        CatalogPriceCalculationLineComponent,
        DomainError> Create(
            Guid calculationLineId,
            Guid needDefinitionId,
            string needName,
            Guid componentProductId,
            Guid manufacturerId,
            string article,
            string name,
            string manufacturerName,
            int quantityPerUnit,
            decimal basePriceAmount,
            decimal discountPercent,
            decimal parentQuantity)
    {
        if (calculationLineId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(calculationLineId));
        }

        if (needDefinitionId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(needDefinitionId));
        }

        if (componentProductId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(componentProductId));
        }

        if (manufacturerId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(manufacturerId));
        }

        var needNameResult = NormalizeRequired(
            needName,
            nameof(needName),
            MaximumNeedNameLength);

        if (needNameResult.IsFailure)
        {
            return needNameResult.Error;
        }

        var articleResult = NormalizeRequired(
            article,
            nameof(article),
            CatalogPriceCalculationLine.MaximumArticleLength);

        if (articleResult.IsFailure)
        {
            return articleResult.Error;
        }

        var nameResult = NormalizeRequired(
            name,
            nameof(name),
            CatalogPriceCalculationLine.MaximumNameLength);

        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        var manufacturerNameResult = NormalizeRequired(
            manufacturerName,
            nameof(manufacturerName),
            CatalogPriceCalculationLine.MaximumManufacturerNameLength);

        if (manufacturerNameResult.IsFailure)
        {
            return manufacturerNameResult.Error;
        }

        var quantityResult =
            ValidateQuantityPerUnit(quantityPerUnit);

        if (quantityResult.IsFailure)
        {
            return quantityResult.Error;
        }

        if (parentQuantity <= 0m
            || parentQuantity
                > CatalogPriceCalculationLine.MaximumQuantity)
        {
            return CatalogPriceCalculationErrors
                .QuantityMustBePositive();
        }

        if (basePriceAmount < 0m)
        {
            return CatalogPriceCalculationErrors
                .PriceCannotBeNegative(
                    nameof(basePriceAmount));
        }

        if (basePriceAmount
            > CatalogPriceCalculationLine.MaximumPriceAmount)
        {
            return CatalogPriceCalculationErrors
                .PriceIsTooLarge(
                    nameof(basePriceAmount),
                    CatalogPriceCalculationLine.MaximumPriceAmount);
        }

        if (discountPercent is < 0m or > 100m)
        {
            return CatalogPriceCalculationErrors
                .DiscountIsOutOfRange();
        }

        return new CatalogPriceCalculationLineComponent(
            Guid.CreateVersion7(),
            calculationLineId,
            needDefinitionId,
            needNameResult.Value,
            componentProductId,
            manufacturerId,
            articleResult.Value,
            nameResult.Value,
            manufacturerNameResult.Value,
            quantityPerUnit,
            decimal.Round(
                basePriceAmount,
                2,
                MidpointRounding.AwayFromZero),
            decimal.Round(
                discountPercent,
                2,
                MidpointRounding.AwayFromZero),
            parentQuantity);
    }

    internal UnitResult<DomainError> ChangeQuantityPerUnit(
        int quantityPerUnit,
        decimal parentQuantity)
    {
        var validationResult =
            ValidateQuantityPerUnit(quantityPerUnit);

        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        QuantityPerUnit = quantityPerUnit;
        Recalculate(parentQuantity);
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    internal UnitResult<DomainError> ApplyDiscount(
        decimal discountPercent,
        decimal parentQuantity)
    {
        if (discountPercent is < 0m or > 100m)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .DiscountIsOutOfRange());
        }

        DiscountPercent = decimal.Round(
            discountPercent,
            2,
            MidpointRounding.AwayFromZero);

        Recalculate(parentQuantity);
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    internal void RecalculateForParentQuantity(
        decimal parentQuantity)
    {
        Recalculate(parentQuantity);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void Recalculate(
        decimal parentQuantity)
    {
        var paymentCoefficient =
            1m - DiscountPercent / 100m;

        ProjectPriceAmount = decimal.Round(
            BasePriceAmount * paymentCoefficient,
            2,
            MidpointRounding.AwayFromZero);

        TotalQuantity =
            parentQuantity * QuantityPerUnit;

        TotalAmount = decimal.Round(
            ProjectPriceAmount * TotalQuantity,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static UnitResult<DomainError>
        ValidateQuantityPerUnit(
            int quantityPerUnit)
    {
        return quantityPerUnit is >= 1
            and <= MaximumQuantityPerUnit
            ? UnitResult.Success<DomainError>()
            : UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .ComponentQuantityPerUnitIsInvalid(
                        MaximumQuantityPerUnit));
    }

    private static Result<string, DomainError>
        NormalizeRequired(
            string value,
            string propertyName,
            int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GeneralErrors.ValueIsRequired(
                propertyName);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            return GeneralErrors.ValueIsTooLong(
                propertyName,
                maximumLength);
        }

        return normalizedValue;
    }
}