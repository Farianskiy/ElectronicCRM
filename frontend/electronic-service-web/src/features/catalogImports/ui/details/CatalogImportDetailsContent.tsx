import type { CatalogImportBatchDetails } from "../../model/types";
import { CatalogImportAppliedProducts } from "../CatalogImportAppliedProducts";
import { CatalogImportBatchHistory } from "../CatalogImportBatchHistory";
import { CatalogImportMappingEditor } from "../CatalogImportMappingEditor";
import { CatalogImportReviewDecisionPanel } from "../CatalogImportReviewDecisionPanel";
import { CatalogImportReviewPanel } from "../CatalogImportReviewPanel";
import { CatalogImportRowsPreview } from "../CatalogImportRowsPreview";
import { CatalogImportSubmitPanel } from "../CatalogImportSubmitPanel";
import { CatalogImportBatchOverview } from "./CatalogImportBatchOverview";
import { CatalogImportDecisionNotices } from "./CatalogImportDecisionNotices";
import { CatalogImportProcessingSummary } from "./CatalogImportProcessingSummary";

interface CatalogImportDetailsContentProps {
  batch: CatalogImportBatchDetails;
}

export function CatalogImportDetailsContent({
  batch,
}: CatalogImportDetailsContentProps) {
  const canEditRows =
    batch.canEdit &&
    (batch.status === "NeedsCorrection" ||
      batch.status === "Ready" ||
      batch.status === "ChangesRequested");

  return (
    <>
      <CatalogImportBatchOverview batch={batch} />

      <CatalogImportDecisionNotices batch={batch} />

      <CatalogImportProcessingSummary batch={batch} />

      <CatalogImportSubmitPanel
        batchId={batch.batchId}
        originalFileName={batch.originalFileName}
        rowsCount={batch.rowsCount}
        validRowsCount={batch.validRowsCount}
        errorRowsCount={batch.errorRowsCount}
        canSubmit={batch.canSubmit}
      />

      {batch.canEdit && <CatalogImportMappingEditor batchId={batch.batchId} />}

      <CatalogImportBatchHistory batchId={batch.batchId} />

      {batch.status === "Applied" && (
        <CatalogImportAppliedProducts batchId={batch.batchId} />
      )}

      <CatalogImportRowsPreview
        batchId={batch.batchId}
        productTypeId={batch.productTypeId}
        canEditRows={canEditRows}
      />

      <CatalogImportReviewPanel
        batchId={batch.batchId}
        originalFileName={batch.originalFileName}
        status={batch.status}
        reviewedByUserId={batch.reviewedByUserId}
        reviewedAtUtc={batch.reviewedAtUtc}
      />

      <CatalogImportReviewDecisionPanel
        batchId={batch.batchId}
        originalFileName={batch.originalFileName}
        rowsCount={batch.rowsCount}
        validRowsCount={batch.validRowsCount}
        errorRowsCount={batch.errorRowsCount}
        canRequestChanges={batch.canRequestChanges}
        canReject={batch.canReject}
        canApply={batch.canApply}
      />
    </>
  );
}
