using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.PriceCalculations;

public sealed class CatalogPriceCalculationManufacturerDiscount
    : ElectronicService.Domain.Abstractions.Entity
{
    private CatalogPriceCalculationManufacturerDiscount(
        Guid id,
        Guid calculationId,
        Guid manufacturerId,
        decimal discountPercent)
        : base(id)
    {
        CalculationId = calculationId;
        ManufacturerId = manufacturerId;
        DiscountPercent = discountPercent;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private CatalogPriceCalculationManufacturerDiscount()
    {
    }

    public Guid CalculationId
    {
        get;
        private set;
    }

    public Guid ManufacturerId
    {
        get;
        private set;
    }

    public decimal DiscountPercent
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
        CatalogPriceCalculationManufacturerDiscount,
        DomainError> Create(
            Guid calculationId,
            Guid manufacturerId,
            decimal discountPercent)
    {
        if (calculationId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceCalculationManufacturerDiscount,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(calculationId)));
        }

        if (manufacturerId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceCalculationManufacturerDiscount,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(manufacturerId)));
        }

        if (discountPercent is < 0m or > 100m)
        {
            return Result.Failure<
                CatalogPriceCalculationManufacturerDiscount,
                DomainError>(
                    CatalogPriceCalculationErrors
                        .DiscountIsOutOfRange());
        }

        return Result.Success<
            CatalogPriceCalculationManufacturerDiscount,
            DomainError>(
                new CatalogPriceCalculationManufacturerDiscount(
                    Guid.CreateVersion7(),
                    calculationId,
                    manufacturerId,
                    decimal.Round(
                        discountPercent,
                        2,
                        MidpointRounding.AwayFromZero)));
    }

    internal UnitResult<DomainError> Change(
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

        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }
}