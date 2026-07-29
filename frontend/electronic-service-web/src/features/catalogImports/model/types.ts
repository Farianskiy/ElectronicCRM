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

export interface CreateCatalogImportBatchResponse {
  batchId: string;
  status: CatalogImportBatchStatus;
}

export interface AnalyzeCatalogImportBatchResponse {
  batchId: string;
  status: CatalogImportBatchStatus;
  productTypeId?: string | null;
  columnsCount: number;
  unmappedColumnsCount: number;
  unconfirmedColumnsCount: number;
  rowsCount: number;
  validRowsCount: number;
  errorRowsCount: number;
}

export const catalogImportRowStatuses = [
  "None",
  "PendingMapping",
  "Valid",
  "Error",
] as const;

export const catalogImportRowFilterStatuses = [
  "PendingMapping",
  "Valid",
  "Error",
] as const;

export type CatalogImportRowStatus =
  (typeof catalogImportRowStatuses)[number];

export type CatalogImportRowFilterStatus =
  (typeof catalogImportRowFilterStatuses)[number];

export interface CatalogImportRowIssue {
  code: string;
  message: string;
  field?: string | null;
  sourceColumnNumber?: number | null;
}

export interface CatalogImportNormalizedRow {
  name?: string | null;
  article?: string | null;
  manufacturer?: string | null;
  manufacturerId?: string | null;
  price?: number | null;
  stockQuantity?: number | null;
  characteristics: Record<string, string>;
}

export interface CatalogImportRow {
  rowId: string;
  rowNumber: number;
  status: CatalogImportRowStatus;

  /*
   * Ключи JSON-объекта в JavaScript всегда строки,
   * даже если backend использует Dictionary<int, string>.
   */
  rawData: Record<string, string>;

  data: CatalogImportNormalizedRow;
  issues: CatalogImportRowIssue[];
  warnings: CatalogImportRowIssue[];
}

export interface GetCatalogImportRowsResponse {
  items: CatalogImportRow[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface GetCatalogImportRowsParams {
  batchId: string;
  status?: CatalogImportRowFilterStatus | null;
  page: number;
  pageSize: number;
}

export const catalogImportColumnTargetKinds = [
  "Unmapped",
  "Ignore",
  "Name",
  "Article",
  "Manufacturer",
  "Price",
  "StockQuantity",
  "Characteristic",
] as const;

export type CatalogImportColumnTargetKind =
  (typeof catalogImportColumnTargetKinds)[number];

export interface CatalogImportMappingColumn {
  columnId: string;
  sourceColumnNumber: number;
  sourceHeader: string;
  targetKind: CatalogImportColumnTargetKind;
  characteristicDefinitionId?: string | null;
  confidence: number;
  isConfirmed: boolean;
}

export interface GetCatalogImportMappingResponse {
  batchId: string;
  status: CatalogImportBatchStatus;
  productTypeId?: string | null;
  columns: CatalogImportMappingColumn[];
  version: number;
  canEdit: boolean;
}

export interface UpdateCatalogImportColumnMappingRequest {
  columnId: string;
  targetKind: CatalogImportColumnTargetKind;
  characteristicDefinitionId?: string | null;
}

export interface UpdateCatalogImportMappingRequest {
  productTypeId: string;
  columns: UpdateCatalogImportColumnMappingRequest[];
}

export interface UpdateCatalogImportMappingResponse {
  batchId: string;
  status: CatalogImportBatchStatus;
  productTypeId: string;
  columnsCount: number;
  unmappedColumnsCount: number;
  unconfirmedColumnsCount: number;
  version: number;
}

export interface UpdateCatalogImportRowRequest {
  name?: string | null;
  article?: string | null;
  manufacturerId?: string | null;
  price?: number | null;
  stockQuantity?: number | null;
  characteristics: Record<string, string>;
}

export interface UpdateCatalogImportRowResponse {
  rowId: string;
  rowNumber: number;
  rowStatus: CatalogImportRowStatus;
  data: CatalogImportNormalizedRow;
  issues: CatalogImportRowIssue[];
  warnings: CatalogImportRowIssue[];
  batchStatus: CatalogImportBatchStatus;
  rowsCount: number;
  validRowsCount: number;
  errorRowsCount: number;
  version: number;
}

export interface SubmitCatalogImportBatchResponse {
  batchId: string;
  status: CatalogImportBatchStatus;
  submittedAtUtc?: string | null;
  version: number;
}

export const catalogImportReviewQueueStatuses = [
  "Submitted",
  "UnderReview",
] as const;

export type CatalogImportReviewQueueStatus =
  (typeof catalogImportReviewQueueStatuses)[number];

export interface CatalogImportReviewQueueItem {
  batchId: string;
  createdByUserId: string;
  createdByDisplayName: string;
  createdByEmail?: string | null;
  createdByUserType: string;
  productTypeId?: string | null;
  originalFileName: string;
  status: CatalogImportReviewQueueStatus;
  rowsCount: number;
  validRowsCount: number;
  errorRowsCount: number;
  createdAtUtc: string;
  submittedAtUtc?: string | null;
  reviewedByUserId?: string | null;
  reviewedAtUtc?: string | null;
  version: number;
}

export interface GetCatalogImportReviewQueueResponse {
  items: CatalogImportReviewQueueItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface GetCatalogImportReviewQueueParams {
  status?: CatalogImportReviewQueueStatus | null;
  page: number;
  pageSize: number;
}

export interface StartCatalogImportReviewResponse {
  batchId: string;
  status: CatalogImportBatchStatus;
  reviewedByUserId?: string | null;
  reviewedAtUtc?: string | null;
  version: number;
}

export interface RequestCatalogImportChangesRequest {
  comment: string;
}

export interface RequestCatalogImportChangesResponse {
  batchId: string;
  status: CatalogImportBatchStatus;
  changesRequestedByUserId?: string | null;
  changesRequestedAtUtc?: string | null;
  comment?: string | null;
  version: number;
}

export interface RejectCatalogImportBatchRequest {
  reason: string;
}

export interface RejectCatalogImportBatchResponse {
  batchId: string;
  status: CatalogImportBatchStatus;
  rejectedByUserId?: string | null;
  rejectedAtUtc?: string | null;
  rejectionReason?: string | null;
  version: number;
}

export interface ApplyCatalogImportBatchResponse {
  batchId: string;
  status: CatalogImportBatchStatus;
  appliedByUserId?: string | null;
  appliedAtUtc?: string | null;
  createdProductsCount: number;
  version: number;
}

export const catalogImportHistoryEventTypes = [
  "Uploaded",
  "Submitted",
  "ReviewStarted",
  "ChangesRequested",
  "Rejected",
  "Applied",
] as const;

export type CatalogImportHistoryEventType =
  (typeof catalogImportHistoryEventTypes)[number];

export interface CatalogImportBatchHistoryItem {
  eventType: CatalogImportHistoryEventType;
  occurredAtUtc: string;
  actorUserId?: string | null;
  actorDisplayName?: string | null;
  actorEmail?: string | null;
  actorUserType?: string | null;
  comment?: string | null;
}

export interface GetCatalogImportBatchHistoryResponse {
  batchId: string;
  items: CatalogImportBatchHistoryItem[];
}

export interface CatalogImportAppliedProduct {
  productId: string;
  article: string;
  name: string;
  productTypeCode: string;
  productTypeName: string;
  manufacturerName: string;
  priceAmount: number;
  priceCurrency: string;
  stockQuantity: number;
  appliedAtUtc: string;
}

export interface GetCatalogImportAppliedProductsResponse {
  batchId: string;
  items: CatalogImportAppliedProduct[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface GetCatalogImportAppliedProductsParams {
  batchId: string;
  page: number;
  pageSize: number;
}