using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.PriceCalculations;

public sealed class CatalogPriceCalculationLine
    : ElectronicService.Domain.Abstractions.Entity
{
    public const int MaximumArticleLength = 100;

    public const int MaximumNameLength = 500;

    public const int MaximumManufacturerNameLength = 200;

    public const int MaximumUnitLength = 50;

    public const decimal MaximumQuantity = 1_000_000m;

    public const decimal MaximumPriceAmount =
        1_000_000_000_000m;

    private CatalogPriceCalculationLine(
        Guid id,
        Guid calculationId,
        Guid productId,
        Guid manufacturerId,
        Guid priceListId,
        Guid priceListRowId,
        string article,
        string name,
        string manufacturerName,
        string? unit,
        decimal quantity,
        decimal basePriceAmount,
        decimal? mrcPriceAmount,
        decimal discountPercent)
        : base(id)
    {
        CalculationId = calculationId;
        ProductId = productId;
        ManufacturerId = manufacturerId;
        PriceListId = priceListId;
        PriceListRowId = priceListRowId;
        Article = article;
        Name = name;
        ManufacturerName = manufacturerName;
        Unit = unit;
        Quantity = quantity;
        BasePriceAmount = basePriceAmount;
        MrcPriceAmount = mrcPriceAmount;
        DiscountPercent = discountPercent;
        CreatedAtUtc = DateTime.UtcNow;

        Recalculate();
    }

    private CatalogPriceCalculationLine()
    {
    }

    public Guid CalculationId
    {
        get;
        private set;
    }

    public Guid ProductId
    {
        get;
        private set;
    }

    public Guid ManufacturerId
    {
        get;
        private set;
    }

    public Guid PriceListId
    {
        get;
        private set;
    }

    public Guid PriceListRowId
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

    public string? Unit
    {
        get;
        private set;
    }

    public decimal Quantity
    {
        get;
        private set;
    }

    public decimal BasePriceAmount
    {
        get;
        private set;
    }

    public decimal? MrcPriceAmount
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
        CatalogPriceCalculationLine,
        DomainError> Create(
            Guid calculationId,
            Guid productId,
            Guid manufacturerId,
            Guid priceListId,
            Guid priceListRowId,
            string article,
            string name,
            string manufacturerName,
            string? unit,
            decimal quantity,
            decimal basePriceAmount,
            decimal? mrcPriceAmount,
            decimal discountPercent)
    {
        if (calculationId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceCalculationLine,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(calculationId)));
        }

        if (productId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceCalculationLine,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(productId)));
        }

        if (manufacturerId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceCalculationLine,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(manufacturerId)));
        }

        if (priceListId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceCalculationLine,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(priceListId)));
        }

        if (priceListRowId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceCalculationLine,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(priceListRowId)));
        }

        if (string.IsNullOrWhiteSpace(article))
        {
            return Result.Failure<
                CatalogPriceCalculationLine,
                DomainError>(
                    GeneralErrors.ValueIsRequired(
                        nameof(article)));
        }

        var normalizedArticle =
            article.Trim();

        if (normalizedArticle.Length
            > MaximumArticleLength)
        {
            return Result.Failure<
                CatalogPriceCalculationLine,
                DomainError>(
                    GeneralErrors.ValueIsTooLong(
                        nameof(article),
                        MaximumArticleLength));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<
                CatalogPriceCalculationLine,
                DomainError>(
                    GeneralErrors.ValueIsRequired(
                        nameof(name)));
        }

        var normalizedName =
            name.Trim();

        if (normalizedName.Length
            > MaximumNameLength)
        {
            return Result.Failure<
                CatalogPriceCalculationLine,
                DomainError>(
                    GeneralErrors.ValueIsTooLong(
                        nameof(name),
                        MaximumNameLength));
        }

        if (string.IsNullOrWhiteSpace(
                manufacturerName))
        {
            return Result.Failure<
                CatalogPriceCalculationLine,
                DomainError>(
                    GeneralErrors.ValueIsRequired(
                        nameof(manufacturerName)));
        }

        var normalizedManufacturerName =
            manufacturerName.Trim();

        if (normalizedManufacturerName.Length
            > MaximumManufacturerNameLength)
        {
            return Result.Failure<
                CatalogPriceCalculationLine,
                DomainError>(
                    GeneralErrors.ValueIsTooLong(
                        nameof(manufacturerName),
                        MaximumManufacturerNameLength));
        }

        var normalizedUnit =
            string.IsNullOrWhiteSpace(unit)
                ? null
                : unit.Trim();

        if (normalizedUnit is not null
            && normalizedUnit.Length
                > MaximumUnitLength)
        {
            return Result.Failure<
                CatalogPriceCalculationLine,
                DomainError>(
                    GeneralErrors.ValueIsTooLong(
                        nameof(unit),
                        MaximumUnitLength));
        }

        var quantityResult =
            ValidateQuantity(quantity);

        if (quantityResult.IsFailure)
        {
            return Result.Failure<
                CatalogPriceCalculationLine,
                DomainError>(
                    quantityResult.Error);
        }

        var basePriceResult =
            ValidatePrice(
                basePriceAmount,
                nameof(basePriceAmount));

        if (basePriceResult.IsFailure)
        {
            return Result.Failure<
                CatalogPriceCalculationLine,
                DomainError>(
                    basePriceResult.Error);
        }

        if (mrcPriceAmount.HasValue)
        {
            var mrcPriceResult =
                ValidatePrice(
                    mrcPriceAmount.Value,
                    nameof(mrcPriceAmount));

            if (mrcPriceResult.IsFailure)
            {
                return Result.Failure<
                    CatalogPriceCalculationLine,
                    DomainError>(
                        mrcPriceResult.Error);
            }
        }

        if (discountPercent is < 0m or > 100m)
        {
            return Result.Failure<
                CatalogPriceCalculationLine,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .DiscountIsOutOfRange());
        }

        return Result.Success<
            CatalogPriceCalculationLine,
            DomainError>(
                new CatalogPriceCalculationLine(
                    Guid.CreateVersion7(),
                    calculationId,
                    productId,
                    manufacturerId,
                    priceListId,
                    priceListRowId,
                    normalizedArticle,
                    normalizedName,
                    normalizedManufacturerName,
                    normalizedUnit,
                    quantity,
                    decimal.Round(
                        basePriceAmount,
                        2,
                        MidpointRounding.AwayFromZero),
                    mrcPriceAmount.HasValue
                        ? decimal.Round(
                            mrcPriceAmount.Value,
                            2,
                            MidpointRounding.AwayFromZero)
                        : null,
                    decimal.Round(
                        discountPercent,
                        2,
                        MidpointRounding.AwayFromZero)));
    }

    internal UnitResult<DomainError> ChangeQuantity(
        decimal quantity)
    {
        var validationResult =
            ValidateQuantity(quantity);

        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        Quantity = quantity;
        Recalculate();
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    internal UnitResult<DomainError> ApplyDiscount(
        decimal discountPercent)
    {
        if (discountPercent is < 0m or > 100m)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .DiscountIsOutOfRange());
        }

        DiscountPercent =
            decimal.Round(
                discountPercent,
                2,
                MidpointRounding.AwayFromZero);

        Recalculate();
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    private static UnitResult<DomainError>
        ValidateQuantity(
            decimal quantity)
    {
        if (quantity <= 0m)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .QuantityMustBePositive());
        }

        if (quantity > MaximumQuantity)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .QuantityIsTooLarge(
                        MaximumQuantity));
        }

        return UnitResult.Success<DomainError>();
    }

    private static UnitResult<DomainError> ValidatePrice(
        decimal price,
        string propertyName)
    {
        if (price < 0m)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .PriceCannotBeNegative(
                        propertyName));
        }

        if (price > MaximumPriceAmount)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .PriceIsTooLarge(
                        propertyName,
                        MaximumPriceAmount));
        }

        return UnitResult.Success<DomainError>();
    }

    private void Recalculate()
    {
        var paymentCoefficient =
            1m - DiscountPercent / 100m;

        ProjectPriceAmount =
            decimal.Round(
                BasePriceAmount
                * paymentCoefficient,
                2,
                MidpointRounding.AwayFromZero);

        TotalAmount =
            decimal.Round(
                ProjectPriceAmount * Quantity,
                2,
                MidpointRounding.AwayFromZero);
    }
}