var currentUser =
    await _userRepository.GetByIdAsync(
        query.CurrentUserId,
        cancellationToken);

if (currentUser is null)
{
    return CatalogImportErrors
        .CurrentUserNotFound();
}

var batch =
    await _importBatchRepository.GetByIdAsync(
        query.BatchId,
        cancellationToken);

if (batch is null)
{
    return CatalogImportErrors
        .BatchNotFound(query.BatchId);
}

var isOwner =
    batch.CreatedByUserId == currentUser.Id;

var canRead =
    isOwner
    || currentUser.CanReviewCatalogImports();

if (!canRead)
{
    return CatalogImportErrors
        .UserCannotAccessBatch();
}

var canEdit =
    isOwner
    && currentUser.CanEditCatalogImport()
    && batch.IsEditable;

var canSubmit =
    isOwner
    && currentUser
        .CanSubmitCatalogImportForReview()
    && batch.Status
        == CatalogImportBatchStatus.Ready;

var canApply =
    currentUser.CanApplyCatalogImport()
    && (
        isOwner
        && batch.Status
            == CatalogImportBatchStatus.Ready
        || batch.Status
            == CatalogImportBatchStatus.UnderReview
    );

return new GetCatalogImportBatchResult(
    batch.Id,
    batch.CreatedByUserId,
    batch.ProductTypeId,
    batch.OriginalFileName,
    batch.FileSizeBytes,
    batch.Status,
    batch.RowsCount,
    batch.ValidRowsCount,
    batch.ErrorRowsCount,
    batch.CreatedAtUtc,
    batch.UpdatedAtUtc,
    batch.SubmittedAtUtc,
    batch.Version,
    canEdit,
    canSubmit,
    canApply);