using System.Security.Cryptography;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.ImportBatches;

public sealed class CatalogImportBatch : AggregateRoot
{
    public const long MaximumFileSizeBytes =
        10 * 1024 * 1024;

    public const int MaximumFileNameLength =
        255;

    public const int MaximumContentTypeLength =
        200;

    public const int MaximumReasonLength =
        2_000;

    private CatalogImportBatch(
        Guid id,
        Guid createdByUserId,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        string fileSha256,
        CatalogImportFile file)
        : base(id)
    {
        CreatedByUserId = createdByUserId;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        FileSha256 = fileSha256;
        File = file;

        Status =
            CatalogImportBatchStatus.Uploaded;

        RowsCount = 0;
        ValidRowsCount = 0;
        ErrorRowsCount = 0;

        CreatedAtUtc = DateTime.UtcNow;
    }

    /*
     * Конструктор для EF Core.
     */
    private CatalogImportBatch()
    {
    }

    public Guid CreatedByUserId
    {
        get;
        private set;
    }

    /*
     * Тип товара может быть неизвестен
     * сразу после загрузки файла.
     *
     * Manager или Technical выберет его
     * на странице сопоставления.
     */
    public Guid? ProductTypeId
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

    public CatalogImportBatchStatus Status
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

    public DateTime? SubmittedAtUtc
    {
        get;
        private set;
    }

    public Guid? ReviewedByUserId
    {
        get;
        private set;
    }

    public DateTime? ReviewedAtUtc
    {
        get;
        private set;
    }

    public Guid? AppliedByUserId
    {
        get;
        private set;
    }

    public DateTime? AppliedAtUtc
    {
        get;
        private set;
    }

    public Guid? RejectedByUserId
    {
        get;
        private set;
    }

    public DateTime? RejectedAtUtc
    {
        get;
        private set;
    }

    public string? RejectionReason
    {
        get;
        private set;
    }

    public string? FailureReason
    {
        get;
        private set;
    }

    /*
     * Оптимистическая блокировка через
     * PostgreSQL xmin.
     */
    public uint Version
    {
        get;
        private set;
    }

    public CatalogImportFile File
    {
        get;
        private set;
    } = null!;

    public bool IsEditable =>
        Status is
            CatalogImportBatchStatus.Uploaded
            or CatalogImportBatchStatus.MappingRequired
            or CatalogImportBatchStatus.NeedsCorrection
            or CatalogImportBatchStatus.Ready;

    public bool IsTerminal =>
        Status is
            CatalogImportBatchStatus.Applied
            or CatalogImportBatchStatus.Rejected
            or CatalogImportBatchStatus.Failed;

    public static Result<
        CatalogImportBatch,
        DomainError> Create(
            Guid createdByUserId,
            string originalFileName,
            string contentType,
            byte[] content)
    {
        if (createdByUserId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(createdByUserId));
        }

        if (string.IsNullOrWhiteSpace(
                originalFileName))
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(originalFileName));
        }

        /*
         * Не доверяем полному пути,
         * который мог прислать клиент.
         */
        var safeFileName =
            Path.GetFileName(
                originalFileName.Trim());

        if (string.IsNullOrWhiteSpace(
                safeFileName)
            || safeFileName.Length
                > MaximumFileNameLength)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(originalFileName));
        }

        var extension =
            Path.GetExtension(safeFileName);

        if (!string.Equals(
                extension,
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            return CatalogImportErrors
                .UnsupportedFileExtension(
                    extension);
        }

        if (string.IsNullOrWhiteSpace(contentType)
            || contentType.Trim().Length
                > MaximumContentTypeLength)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(contentType));
        }

        ArgumentNullException.ThrowIfNull(content);

        if (content.Length == 0)
        {
            return CatalogImportErrors.FileIsEmpty();
        }

        if (content.LongLength
            > MaximumFileSizeBytes)
        {
            return CatalogImportErrors
                .FileIsTooLarge(
                    MaximumFileSizeBytes);
        }

        var batchId =
            Guid.CreateVersion7();

        var fileResult =
            CatalogImportFile.Create(
                batchId,
                content);

        if (fileResult.IsFailure)
        {
            return fileResult.Error;
        }

        /*
         * SHA-256 позволит:
         *
         * 1. обнаруживать повторную загрузку;
         * 2. проверять целостность файла;
         * 3. связывать одинаковые исходники.
         */
        var sha256 =
            Convert.ToHexString(
                SHA256.HashData(content));

        return new CatalogImportBatch(
            batchId,
            createdByUserId,
            safeFileName,
            contentType.Trim(),
            content.LongLength,
            sha256,
            fileResult.Value);
    }

    public UnitResult<DomainError>
        AssignProductType(
            Guid productTypeId)
    {
        if (productTypeId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(productTypeId)));
        }

        if (!IsEditable)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .InvalidStatusTransition(
                        Status,
                        CatalogImportBatchStatus.Uploaded));
        }

        if (ProductTypeId == productTypeId)
        {
            return UnitResult.Success<DomainError>();
        }

        ProductTypeId = productTypeId;

        /*
         * При смене типа предыдущий анализ
         * больше нельзя считать актуальным.
         */
        Status =
            CatalogImportBatchStatus.Uploaded;

        RowsCount = 0;
        ValidRowsCount = 0;
        ErrorRowsCount = 0;

        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError>
        RegisterAnalysisResult(
            int rowsCount,
            int validRowsCount,
            int errorRowsCount,
            bool mappingRequired)
    {
        if (!IsEditable)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .InvalidStatusTransition(
                        Status,
                        CatalogImportBatchStatus
                            .NeedsCorrection));
        }

        if (rowsCount < 0
            || validRowsCount < 0
            || errorRowsCount < 0
            || validRowsCount > rowsCount
            || errorRowsCount > rowsCount
            || validRowsCount + errorRowsCount
                > rowsCount)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .InvalidRowsStatistics());
        }

        RowsCount = rowsCount;
        ValidRowsCount = validRowsCount;
        ErrorRowsCount = errorRowsCount;

        if (mappingRequired
            || ProductTypeId is null)
        {
            Status =
                CatalogImportBatchStatus
                    .MappingRequired;
        }
        else if (errorRowsCount > 0)
        {
            Status =
                CatalogImportBatchStatus
                    .NeedsCorrection;
        }
        else
        {
            Status =
                CatalogImportBatchStatus.Ready;
        }

        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError>
        SubmitForReview()
    {
        if (Status
            != CatalogImportBatchStatus.Ready)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .InvalidStatusTransition(
                        Status,
                        CatalogImportBatchStatus
                            .Submitted));
        }

        if (ProductTypeId is null)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .ProductTypeIsRequired());
        }

        Status =
            CatalogImportBatchStatus.Submitted;

        SubmittedAtUtc = DateTime.UtcNow;

        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError>
        StartReview(
            Guid reviewedByUserId)
    {
        if (reviewedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(reviewedByUserId)));
        }

        if (Status
            != CatalogImportBatchStatus.Submitted)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .InvalidStatusTransition(
                        Status,
                        CatalogImportBatchStatus
                            .UnderReview));
        }

        Status =
            CatalogImportBatchStatus.UnderReview;

        ReviewedByUserId = reviewedByUserId;
        ReviewedAtUtc = DateTime.UtcNow;

        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError>
        StartApplying(
            Guid appliedByUserId)
    {
        if (appliedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(appliedByUserId)));
        }

        /*
         * Ready:
         * Technical применяет собственный batch.
         *
         * UnderReview:
         * Technical применяет batch Manager.
         */
        if (Status is not
            CatalogImportBatchStatus.Ready
            and not
            CatalogImportBatchStatus.UnderReview)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .InvalidStatusTransition(
                        Status,
                        CatalogImportBatchStatus
                            .Applying));
        }

        if (ProductTypeId is null)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .ProductTypeIsRequired());
        }

        if (ErrorRowsCount > 0)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .InvalidRowsStatistics());
        }

        Status =
            CatalogImportBatchStatus.Applying;

        AppliedByUserId = appliedByUserId;

        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError>
        CompleteApplying()
    {
        if (Status
            != CatalogImportBatchStatus.Applying)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .InvalidStatusTransition(
                        Status,
                        CatalogImportBatchStatus
                            .Applied));
        }

        Status =
            CatalogImportBatchStatus.Applied;

        AppliedAtUtc = DateTime.UtcNow;

        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> Reject(
        Guid rejectedByUserId,
        string rejectionReason)
    {
        if (rejectedByUserId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(rejectedByUserId)));
        }

        if (string.IsNullOrWhiteSpace(
                rejectionReason))
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .RejectionReasonIsRequired());
        }

        if (Status is not
            CatalogImportBatchStatus.Submitted
            and not
            CatalogImportBatchStatus.UnderReview)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .InvalidStatusTransition(
                        Status,
                        CatalogImportBatchStatus
                            .Rejected));
        }

        var normalizedReason =
            rejectionReason.Trim();

        if (normalizedReason.Length
            > MaximumReasonLength)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(rejectionReason)));
        }

        Status =
            CatalogImportBatchStatus.Rejected;

        RejectedByUserId = rejectedByUserId;
        RejectedAtUtc = DateTime.UtcNow;
        RejectionReason = normalizedReason;

        Touch();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> MarkFailed(
        string failureReason)
    {
        if (string.IsNullOrWhiteSpace(
                failureReason))
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .FailureReasonIsRequired());
        }

        if (IsTerminal)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .InvalidStatusTransition(
                        Status,
                        CatalogImportBatchStatus
                            .Failed));
        }

        var normalizedReason =
            failureReason.Trim();

        if (normalizedReason.Length
            > MaximumReasonLength)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(failureReason)));
        }

        Status =
            CatalogImportBatchStatus.Failed;

        FailureReason = normalizedReason;

        Touch();

        return UnitResult.Success<DomainError>();
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}