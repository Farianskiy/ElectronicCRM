using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.PriceCalculations;

public sealed class CatalogPriceCalculation : AggregateRoot
{
    public const int MaximumTitleLength = 200;

    public const int MaximumCustomerNameLength = 200;

    public const int MaximumObjectNameLength = 300;

    public const int MaximumProjectNumberLength = 100;

    public const int MaximumResponsibleNameLength = 200;

    public const int MaximumCommentLength = 2000;

    public const string DefaultCurrency = "RUB";

    private readonly List<CatalogPriceCalculationLine>
        _lines = [];

    private readonly List<
        CatalogPriceCalculationManufacturerDiscount>
        _manufacturerDiscounts = [];

    private CatalogPriceCalculation(
        Guid id,
        Guid createdByUserId,
        string title)
        : base(id)
    {
        CreatedByUserId = createdByUserId;
        Title = title;
        Currency = DefaultCurrency;
        Status = CatalogPriceCalculationStatus.Draft;
        TotalAmount = 0m;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private CatalogPriceCalculation()
    {
    }

    public Guid CreatedByUserId
    {
        get;
        private set;
    }

    public string Title
    {
        get;
        private set;
    } = string.Empty;

    public string Currency
    {
        get;
        private set;
    } = DefaultCurrency;

    public string? CustomerName
    {
        get;
        private set;
    }

    public string? ObjectName
    {
        get;
        private set;
    }

    public string? ProjectNumber
    {
        get;
        private set;
    }

    public string? ResponsibleName
    {
        get;
        private set;
    }

    public string? Comment
    {
        get;
        private set;
    }

    public DateOnly? ValidUntil
    {
        get;
        private set;
    }

    public CatalogPriceCalculationStatus Status
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

    public DateTime? CompletedAtUtc
    {
        get;
        private set;
    }

    public DateTime? ArchivedAtUtc
    {
        get;
        private set;
    }

    public uint Version
    {
        get;
        private set;
    }

    public IReadOnlyCollection<
        CatalogPriceCalculationLine> Lines =>
            _lines;

    public IReadOnlyCollection<
        CatalogPriceCalculationManufacturerDiscount>
        ManufacturerDiscounts =>
            _manufacturerDiscounts;

    public bool IsEditable =>
        Status == CatalogPriceCalculationStatus.Draft;

    public static Result<
        CatalogPriceCalculation,
        DomainError> Create(
            Guid createdByUserId,
            string title)
    {
        if (createdByUserId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceCalculation,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(createdByUserId)));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<
                CatalogPriceCalculation,
                DomainError>(
                    GeneralErrors.ValueIsRequired(
                        nameof(title)));
        }

        var normalizedTitle =
            title.Trim();

        if (normalizedTitle.Length
            > MaximumTitleLength)
        {
            return Result.Failure<
                CatalogPriceCalculation,
                DomainError>(
                    GeneralErrors.ValueIsTooLong(
                        nameof(title),
                        MaximumTitleLength));
        }

        return Result.Success<
            CatalogPriceCalculation,
            DomainError>(
                new CatalogPriceCalculation(
                    Guid.CreateVersion7(),
                    createdByUserId,
                    normalizedTitle));
    }

    public UnitResult<DomainError> Rename(
        string title)
    {
        var editableResult =
            EnsureEditable();

        if (editableResult.IsFailure)
        {
            return editableResult;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsRequired(
                    nameof(title)));
        }

        var normalizedTitle =
            title.Trim();

        if (normalizedTitle.Length
            > MaximumTitleLength)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsTooLong(
                    nameof(title),
                    MaximumTitleLength));
        }

        Title = normalizedTitle;
        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> UpdateProjectCard(
        string? customerName,
        string? objectName,
        string? projectNumber,
        string? responsibleName,
        string? comment,
        DateOnly? validUntil)
    {
        var editableResult = EnsureEditable();

        if (editableResult.IsFailure)
        {
            return editableResult;
        }

        var customerNameResult = NormalizeOptional(customerName, nameof(customerName), MaximumCustomerNameLength);
        var objectNameResult = NormalizeOptional(objectName, nameof(objectName), MaximumObjectNameLength);
        var projectNumberResult = NormalizeOptional(projectNumber, nameof(projectNumber), MaximumProjectNumberLength);
        var responsibleNameResult = NormalizeOptional(responsibleName, nameof(responsibleName), MaximumResponsibleNameLength);
        var commentResult = NormalizeOptional(comment, nameof(comment), MaximumCommentLength);

        if (customerNameResult.IsFailure)
        {
            return UnitResult.Failure(customerNameResult.Error);
        }

        if (objectNameResult.IsFailure)
        {
            return UnitResult.Failure(objectNameResult.Error);
        }

        if (projectNumberResult.IsFailure)
        {
            return UnitResult.Failure(projectNumberResult.Error);
        }

        if (responsibleNameResult.IsFailure)
        {
            return UnitResult.Failure(responsibleNameResult.Error);
        }

        if (commentResult.IsFailure)
        {
            return UnitResult.Failure(commentResult.Error);
        }

        CustomerName = customerNameResult.Value;
        ObjectName = objectNameResult.Value;
        ProjectNumber = projectNumberResult.Value;
        ResponsibleName = responsibleNameResult.Value;
        Comment = commentResult.Value;
        ValidUntil = validUntil;
        Touch();

        return UnitResult.Success<DomainError>();
    }

    public Result<Guid, DomainError> AddLine(
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
        decimal? mrcPriceAmount)
    {
        var editableResult =
            EnsureEditable();

        if (editableResult.IsFailure)
        {
            return Result.Failure<Guid, DomainError>(
                editableResult.Error);
        }

        var duplicateExists =
            _lines.Any(
                line =>
                    line.PriceListRowId
                    == priceListRowId);

        if (duplicateExists)
        {
            return Result.Failure<Guid, DomainError>(
                CatalogPriceCalculationErrors
                    .DuplicatePriceListRow(
                        priceListRowId));
        }

        var discountPercent =
            _manufacturerDiscounts
                .SingleOrDefault(
                    discount =>
                        discount.ManufacturerId
                        == manufacturerId)
                ?.DiscountPercent
            ?? 0m;

        var lineResult =
            CatalogPriceCalculationLine.Create(
                Id,
                productId,
                manufacturerId,
                priceListId,
                priceListRowId,
                article,
                name,
                manufacturerName,
                unit,
                quantity,
                basePriceAmount,
                mrcPriceAmount,
                discountPercent);

        if (lineResult.IsFailure)
        {
            return Result.Failure<Guid, DomainError>(
                lineResult.Error);
        }

        _lines.Add(lineResult.Value);

        RecalculateTotal();
        Touch();

        return Result.Success<Guid, DomainError>(
            lineResult.Value.Id);
    }

    public UnitResult<DomainError> RemoveLine(
        Guid lineId)
    {
        var editableResult =
            EnsureEditable();

        if (editableResult.IsFailure)
        {
            return editableResult;
        }

        if (lineId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(lineId)));
        }

        var line =
            _lines.SingleOrDefault(
                existingLine =>
                    existingLine.Id == lineId);

        if (line is null)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .LineNotFound(lineId));
        }

        var manufacturerId =
            line.ManufacturerId;

        _lines.Remove(line);

        var manufacturerStillHasLines =
            _lines.Any(
                existingLine =>
                    existingLine.ManufacturerId
                    == manufacturerId);

        if (!manufacturerStillHasLines)
        {
            var discount =
                _manufacturerDiscounts
                    .SingleOrDefault(
                        existingDiscount =>
                            existingDiscount
                                .ManufacturerId
                            == manufacturerId);

            if (discount is not null)
            {
                _manufacturerDiscounts.Remove(
                    discount);
            }
        }

        RecalculateTotal();
        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> ChangeLineQuantity(
        Guid lineId,
        decimal quantity)
    {
        var editableResult =
            EnsureEditable();

        if (editableResult.IsFailure)
        {
            return editableResult;
        }

        if (lineId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(lineId)));
        }

        var line =
            _lines.SingleOrDefault(
                existingLine =>
                    existingLine.Id == lineId);

        if (line is null)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .LineNotFound(lineId));
        }

        var changeResult =
            line.ChangeQuantity(quantity);

        if (changeResult.IsFailure)
        {
            return changeResult;
        }

        RecalculateTotal();
        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError>
        SetManufacturerDiscount(
            Guid manufacturerId,
            decimal discountPercent)
    {
        var editableResult =
            EnsureEditable();

        if (editableResult.IsFailure)
        {
            return editableResult;
        }

        if (manufacturerId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(manufacturerId)));
        }

        if (discountPercent is < 0m or > 100m)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .DiscountIsOutOfRange());
        }

        var manufacturerLines =
            _lines
                .Where(
                    line =>
                        line.ManufacturerId
                        == manufacturerId)
                .ToArray();

        if (manufacturerLines.Length == 0)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .ManufacturerHasNoLines(
                        manufacturerId));
        }

        var discount =
            _manufacturerDiscounts
                .SingleOrDefault(
                    existingDiscount =>
                        existingDiscount.ManufacturerId
                        == manufacturerId);

        if (discount is null)
        {
            var discountResult =
                CatalogPriceCalculationManufacturerDiscount
                    .Create(
                        Id,
                        manufacturerId,
                        discountPercent);

            if (discountResult.IsFailure)
            {
                return UnitResult.Failure(
                    discountResult.Error);
            }

            discount = discountResult.Value;

            _manufacturerDiscounts.Add(discount);
        }
        else
        {
            var discountChangeResult =
                discount.Change(discountPercent);

            if (discountChangeResult.IsFailure)
            {
                return discountChangeResult;
            }
        }

        foreach (var line in manufacturerLines)
        {
            var lineResult =
                line.ApplyDiscount(
                    discount.DiscountPercent);

            if (lineResult.IsFailure)
            {
                return lineResult;
            }
        }

        RecalculateTotal();
        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError>
        RemoveManufacturerDiscount(
            Guid manufacturerId)
    {
        var editableResult =
            EnsureEditable();

        if (editableResult.IsFailure)
        {
            return editableResult;
        }

        if (manufacturerId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(manufacturerId)));
        }

        var discount =
            _manufacturerDiscounts
                .SingleOrDefault(
                    existingDiscount =>
                        existingDiscount.ManufacturerId
                        == manufacturerId);

        if (discount is null)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .DiscountNotFound(
                        manufacturerId));
        }

        var manufacturerLines =
            _lines
                .Where(
                    line =>
                        line.ManufacturerId
                        == manufacturerId)
                .ToArray();

        foreach (var line in manufacturerLines)
        {
            var lineResult =
                line.ApplyDiscount(0m);

            if (lineResult.IsFailure)
            {
                return lineResult;
            }
        }

        _manufacturerDiscounts.Remove(discount);

        RecalculateTotal();
        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Complete()
    {
        if (Status
            != CatalogPriceCalculationStatus.Draft)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .InvalidStatusTransition(
                        Status,
                        CatalogPriceCalculationStatus
                            .Completed));
        }

        if (_lines.Count == 0)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .CalculationMustContainLines());
        }

        Status =
            CatalogPriceCalculationStatus.Completed;

        CompletedAtUtc = DateTime.UtcNow;
        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Archive()
    {
        if (Status
            != CatalogPriceCalculationStatus.Completed)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .InvalidStatusTransition(
                        Status,
                        CatalogPriceCalculationStatus
                            .Archived));
        }

        Status =
            CatalogPriceCalculationStatus.Archived;

        ArchivedAtUtc = DateTime.UtcNow;
        Touch();

        return UnitResult.Success<DomainError>();
    }

    private UnitResult<DomainError> EnsureEditable()
    {
        if (!IsEditable)
        {
            return UnitResult.Failure(
                CatalogPriceCalculationErrors
                    .CannotModifyCalculation(
                        Status));
        }

        return UnitResult.Success<DomainError>();
    }

    private static Result<string?, DomainError> NormalizeOptional(
        string? value,
        string propertyName,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Success<string?, DomainError>(null);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            return Result.Failure<string?, DomainError>(GeneralErrors.ValueIsTooLong(propertyName, maximumLength));
        }

        return Result.Success<string?, DomainError>(normalizedValue);
    }

    private void RecalculateTotal()
    {
        TotalAmount =
            decimal.Round(
                _lines.Sum(
                    line =>
                        line.TotalAmount),
                2,
                MidpointRounding.AwayFromZero);
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}