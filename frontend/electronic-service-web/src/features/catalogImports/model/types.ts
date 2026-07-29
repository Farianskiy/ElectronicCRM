export const catalogImportBatchStatuses = [
  "Uploaded",
  "MappingRequired",
  "NeedsCorrection",
  "Ready",
  "Submitted",
  "UnderReview",
  "Applying",
  "Applied",
  "Rejected",
  "Failed",
  "ChangesRequested",
] as const;

export type CatalogImportBatchStatus =
  (typeof catalogImportBatchStatuses)[number];

export interface MyCatalogImportBatchItem {
  batchId: string;
  productTypeId?: string | null;
  originalFileName: string;
  fileSizeBytes: number;
  status: CatalogImportBatchStatus;
  rowsCount: number;
  validRowsCount: number;
  errorRowsCount: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  lastActivityAtUtc: string;
  submittedAtUtc?: string | null;
  changesRequestedAtUtc?: string | null;
  changesRequestComment?: string | null;
  rejectedAtUtc?: string | null;
  rejectionReason?: string | null;
  appliedAtUtc?: string | null;
  version: number;
  canEdit: boolean;
  canSubmit: boolean;
  canApply: boolean;
  canDelete: boolean;
}

export interface GetMyCatalogImportBatchesResponse {
  items: MyCatalogImportBatchItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface GetMyCatalogImportBatchesParams {
  status?: CatalogImportBatchStatus | null;
  page: number;
  pageSize: number;
}

export interface CatalogImportBatchDetails {
  batchId: string;
  createdByUserId: string;
  productTypeId?: string | null;
  originalFileName: string;
  fileSizeBytes: number;
  status: CatalogImportBatchStatus;
  rowsCount: number;
  validRowsCount: number;
  errorRowsCount: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  submittedAtUtc?: string | null;
  reviewedByUserId?: string | null;
  reviewedAtUtc?: string | null;
  changesRequestedByUserId?: string | null;
  changesRequestedAtUtc?: string | null;
  changesRequestComment?: string | null;
  rejectedByUserId?: string | null;
  rejectedAtUtc?: string | null;
  rejectionReason?: string | null;
  appliedByUserId?: string | null;
  appliedAtUtc?: string | null;
  version: number;
  canEdit: boolean;
  canSubmit: boolean;
  canApply: boolean;
  canRequestChanges: boolean;
  canReject: boolean;
  canDownloadFile: boolean;
  canDelete: boolean;
}