using System.Security.Cryptography;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.PriceLists;

public sealed class CatalogPriceList : AggregateRoot
{
    public const long MaximumFileSizeBytes =
        30 * 1024 * 1024;

    public const long MaximumExtractedWorkbookSizeBytes =
        100 * 1024 * 1024;

    public const int MaximumFileNameLength = 255;

    public const int MaximumContentTypeLength = 200;

    public const int MaximumFailureReasonLength = 2_000;

    public const int MaximumRowsCount = 250_000;

    public const string DefaultCurrency = "RUB";

    public const decimal DefaultVatRatePercent = 22m;

    private CatalogPriceList(
        Guid id,
        Guid manufacturerId,
        Guid createdByUserId,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        string fileSha256,
        CatalogPriceListFile file)
        : base(id)
    {
        ManufacturerId = manufacturerId;
        CreatedByUserId = createdByUserId;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        FileSha256 = fileSha256;
        File = file;
        Currency = DefaultCurrency;
        VatRatePercent = DefaultVatRatePercent;
        Status = CatalogPriceListStatus.Uploaded;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private CatalogPriceList()
    {
    }

    public Guid ManufacturerId
    {
        get;
        private set;
    }

    public Guid CreatedByUserId
    {
        get;
        private set;
    }

    public string OriginalFileName
    {
        get;
        private set;
    } = null!;

    public string ContentType
    {
        get;
        private set;
    } = null!;

    public long FileSizeBytes
    {
        get;
        private set;
    }

    public string FileSha256
    {
        get;
        private set;
    } = null!;

    public string Currency
    {
        get;
        private set;
    } = DefaultCurrency;

    public decimal VatRatePercent
    {
        get;
        private set;
    }

    public DateOnly? EffectiveDate
    {
        get;
        private set;
    }

    public CatalogPriceListStatus Status
    {
        get;
        private set;
    }

    public int RowsCount
    {
        get;
        private set;
    }

    public int ValidRowsCount
    {
        get;
        private set;
    }

    public int ErrorRowsCount
    {
        get;
        private set;
    }

    public int EstimatedRowsCount
    {
        get;
        private set;
    }

    public int ReadRowsCount
    {
        get;
        private set;
    }

    public int SavedRowsCount
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

    public DateTime? ProcessedAtUtc
    {
        get;
        private set;
    }

    public DateTime? ActivatedAtUtc
    {
        get;
        private set;
    }

    public DateTime? ArchivedAtUtc
    {
        get;
        private set;
    }

    public string? FailureReason
    {
        get;
        private set;
    }

    public uint Version
    {
        get;
        private set;
    }

    public CatalogPriceListFile File
    {
        get;
        private set;
    } = null!;

    public static Result<CatalogPriceList, DomainError> Create(
        Guid manufacturerId,
        Guid createdByUserId,
        string originalFileName,
        string contentType,
        byte[] content)
    {
        if (manufacturerId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(manufacturerId));
        }

        if (createdByUserId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(createdByUserId));
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return GeneralErrors.ValueIsRequired(nameof(originalFileName));
        }

        var safeFileName =
            Path.GetFileName(originalFileName.Trim());

        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            return GeneralErrors.ValueIsInvalid(nameof(originalFileName));
        }

        if (safeFileName.Length > MaximumFileNameLength)
        {
            return GeneralErrors.ValueIsTooLong(
                nameof(originalFileName),
                MaximumFileNameLength);
        }

        var extension =
            Path.GetExtension(safeFileName);

        var extensionIsSupported =
            string.Equals(
                extension,
                ".zip",
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                extension,
                ".xlsx",
                StringComparison.OrdinalIgnoreCase);

        if (!extensionIsSupported)
        {
            return CatalogPriceListErrors.UnsupportedFileExtension(
                extension);
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return GeneralErrors.ValueIsRequired(nameof(contentType));
        }

        var normalizedContentType =
            contentType.Trim();

        if (normalizedContentType.Length > MaximumContentTypeLength)
        {
            return GeneralErrors.ValueIsTooLong(
                nameof(contentType),
                MaximumContentTypeLength);
        }

        ArgumentNullException.ThrowIfNull(content);

        if (content.Length == 0)
        {
            return CatalogPriceListErrors.FileIsEmpty();
        }

        if (content.LongLength > MaximumFileSizeBytes)
        {
            return CatalogPriceListErrors.FileIsTooLarge(
                MaximumFileSizeBytes);
        }

        var priceListId =
            Guid.CreateVersion7();

        var fileResult =
            CatalogPriceListFile.Create(
                priceListId,
                content);

        if (fileResult.IsFailure)
        {
            return fileResult.Error;
        }

        var sha256 =
            Convert.ToHexString(
                SHA256.HashData(content));

        return new CatalogPriceList(
            priceListId,
            manufacturerId,
            createdByUserId,
            safeFileName,
            normalizedContentType,
            content.LongLength,
            sha256,
            fileResult.Value);
    }

    public UnitResult<DomainError> StartProcessing()
    {
        if (Status is not CatalogPriceListStatus.Uploaded
            and not CatalogPriceListStatus.NeedsCorrection
            and not CatalogPriceListStatus.Failed)
        {
            return UnitResult.Failure(
                CatalogPriceListErrors.InvalidStatusTransition(
                    Status,
                    CatalogPriceListStatus.Processing));
        }

        Status = CatalogPriceListStatus.Processing;
        FailureReason = null;
        ProcessedAtUtc = null;
        RowsCount = 0;
        ValidRowsCount = 0;
        ErrorRowsCount = 0;
        EstimatedRowsCount = 0;
        ReadRowsCount = 0;
        SavedRowsCount = 0;
        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> CompleteProcessing(
        DateOnly effectiveDate,
        int rowsCount,
        int validRowsCount,
        int errorRowsCount)
    {
        if (Status != CatalogPriceListStatus.Processing)
        {
            return UnitResult.Failure(
                CatalogPriceListErrors.InvalidStatusTransition(
                    Status,
                    CatalogPriceListStatus.Ready));
        }

        if (effectiveDate == default)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(effectiveDate)));
        }

        if (rowsCount <= 0
            || rowsCount > MaximumRowsCount
            || validRowsCount < 0
            || errorRowsCount < 0
            || validRowsCount + errorRowsCount != rowsCount)
        {
            return UnitResult.Failure(
                CatalogPriceListErrors.InvalidRowsStatistics());
        }

        EffectiveDate = effectiveDate;
        RowsCount = rowsCount;
        ValidRowsCount = validRowsCount;
        ErrorRowsCount = errorRowsCount;
        EstimatedRowsCount = rowsCount;
        ReadRowsCount = rowsCount;
        SavedRowsCount = rowsCount;
        ProcessedAtUtc = DateTime.UtcNow;
        Status = errorRowsCount == 0
            ? CatalogPriceListStatus.Ready
            : CatalogPriceListStatus.NeedsCorrection;
        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> RefreshRowsStatistics(
    int rowsCount,
    int validRowsCount,
    int errorRowsCount)
    {
        if (Status is not CatalogPriceListStatus.NeedsCorrection
            and not CatalogPriceListStatus.Ready)
        {
            return UnitResult.Failure(
                CatalogPriceListErrors.RowsCannotBeEdited(
                    Status));
        }

        if (rowsCount <= 0
            || rowsCount > MaximumRowsCount
            || validRowsCount < 0
            || errorRowsCount < 0
            || validRowsCount + errorRowsCount
                != rowsCount)
        {
            return UnitResult.Failure(
                CatalogPriceListErrors.InvalidRowsStatistics());
        }

        RowsCount = rowsCount;
        ValidRowsCount = validRowsCount;
        ErrorRowsCount = errorRowsCount;
        Status =
            errorRowsCount == 0
                ? CatalogPriceListStatus.Ready
                : CatalogPriceListStatus.NeedsCorrection;

        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Activate()
    {
        if (Status != CatalogPriceListStatus.Ready)
        {
            return UnitResult.Failure(
                CatalogPriceListErrors.InvalidStatusTransition(
                    Status,
                    CatalogPriceListStatus.Active));
        }

        Status = CatalogPriceListStatus.Active;
        ActivatedAtUtc = DateTime.UtcNow;
        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Archive()
    {
        if (Status != CatalogPriceListStatus.Active)
        {
            return UnitResult.Failure(
                CatalogPriceListErrors.InvalidStatusTransition(
                    Status,
                    CatalogPriceListStatus.Archived));
        }

        Status = CatalogPriceListStatus.Archived;
        ArchivedAtUtc = DateTime.UtcNow;
        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> MarkFailed(
        string failureReason)
    {
        if (Status is CatalogPriceListStatus.Active
            or CatalogPriceListStatus.Archived)
        {
            return UnitResult.Failure(
                CatalogPriceListErrors.InvalidStatusTransition(
                    Status,
                    CatalogPriceListStatus.Failed));
        }

        if (string.IsNullOrWhiteSpace(failureReason))
        {
            return UnitResult.Failure(
                CatalogPriceListErrors.FailureReasonIsRequired());
        }

        var normalizedFailureReason =
            failureReason.Trim();

        if (normalizedFailureReason.Length > MaximumFailureReasonLength)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsTooLong(
                    nameof(failureReason),
                    MaximumFailureReasonLength));
        }

        Status = CatalogPriceListStatus.Failed;
        FailureReason = normalizedFailureReason;
        Touch();

        return UnitResult.Success<DomainError>();
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}