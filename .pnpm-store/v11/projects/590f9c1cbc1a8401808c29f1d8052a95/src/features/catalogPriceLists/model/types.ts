export const catalogPriceListStatuses = [
  "Uploaded",
  "Processing",
  "NeedsCorrection",
  "Ready",
  "Active",
  "Archived",
  "Failed",
] as const;

export type CatalogPriceListStatus =
  (typeof catalogPriceListStatuses)[number];

export interface CatalogPriceListVersion {
  priceListId: string;
  originalFileName: string;
  fileSizeBytes: number;
  effectiveDate?: string | null;
  status: CatalogPriceListStatus;
  rowsCount: number;
  validRowsCount: number;
  errorRowsCount: number;
  estimatedRowsCount: number;
  readRowsCount: number;
  savedRowsCount: number;
  createdAtUtc: string;
  processedAtUtc?: string | null;
  activatedAtUtc?: string | null;
  archivedAtUtc?: string | null;
  failureReason?: string | null;
}

export interface GetCatalogPriceListVersionsParams {
  manufacturerId: string;
  status?: CatalogPriceListStatus | null;
  page: number;
  pageSize: number;
}

export interface GetCatalogPriceListVersionsResponse {
  manufacturerId: string;
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: CatalogPriceListVersion[];
}

export interface UploadCatalogPriceListRequest {
  manufacturerId: string;
  file: File;
}

export interface UploadCatalogPriceListResponse {
  priceListId: string;
  manufacturerId: string;
  originalFileName: string;
  fileSizeBytes: number;
  status: CatalogPriceListStatus;
}

export const catalogPriceListRowStatuses = [
  "Pending",
  "Valid",
  "Error",
] as const;

export type CatalogPriceListRowStatus =
  (typeof catalogPriceListRowStatuses)[number];

export const catalogPriceListRowMatchStatuses = [
  "Pending",
  "MatchedByArticle",
  "MatchedByName",
  "MatchedManually",
  "Ambiguous",
  "ProductNotFound",
] as const;

export type CatalogPriceListRowMatchStatus =
  (typeof catalogPriceListRowMatchStatuses)[number];

export interface CatalogPriceListDetails {
  priceListId: string;
  manufacturerId: string;
  manufacturerName: string;
  createdByUserId: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  currency: string;
  vatRatePercent: number;
  effectiveDate?: string | null;
  status: CatalogPriceListStatus;
  rowsCount: number;
  validRowsCount: number;
  errorRowsCount: number;
  estimatedRowsCount: number;
  readRowsCount: number;
  savedRowsCount: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  processedAtUtc?: string | null;
  activatedAtUtc?: string | null;
  archivedAtUtc?: string | null;
  failureReason?: string | null;
}

export interface CatalogPriceListRowIssue {
  code: string;
  field: string;
  message: string;
}

export interface CatalogPriceListRow {
  rowId: string;
  rowNumber: number;
  article: string;
  name: string;
  basePriceAmount?: number | null;
  mrcPriceAmount?: number | null;
  productUrl?: string | null;
  unit?: string | null;
  productId?: string | null;
  status: CatalogPriceListRowStatus;
  matchStatus: CatalogPriceListRowMatchStatus;
  matchConfidencePercent?: number | null;
  issues: CatalogPriceListRowIssue[];
}

export interface GetCatalogPriceListRowsParams {
  priceListId: string;
  status?: CatalogPriceListRowStatus | null;
  matchStatus?: CatalogPriceListRowMatchStatus | null;
  issueCode?: string | null;
  page: number;
  pageSize: number;
}

export interface GetCatalogPriceListRowsResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: CatalogPriceListRow[];
}

export interface ProcessCatalogPriceListResponse {
  priceListId: string;
  rowsCount: number;
  validRowsCount: number;
  errorRowsCount: number;
  status: CatalogPriceListStatus;
}

export interface ActivateCatalogPriceListResponse {
  priceListId: string;
  manufacturerId: string;
  status: CatalogPriceListStatus;
  activatedAtUtc?: string | null;
  archivedPriceListId?: string | null;
}

export interface CatalogPriceListProductSearchItem {
  productId: string;
  article: string;
  name: string;
  productTypeCode: string;
  productTypeName: string;
}

export interface SearchCatalogPriceListProductsParams {
  priceListId: string;
  search?: string | null;
  page: number;
  pageSize: number;
}

export interface SearchCatalogPriceListProductsResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: CatalogPriceListProductSearchItem[];
}

export interface UpdateCatalogPriceListRowRequest {
  article: string;
  name: string;
  basePriceAmount?: number | null;
  mrcPriceAmount?: number | null;
  productUrl?: string | null;
  unit?: string | null;
  productId?: string | null;
}

export interface UpdateCatalogPriceListRowResponse {
  priceListId: string;
  priceListStatus: CatalogPriceListStatus;
  rowsCount: number;
  validRowsCount: number;
  errorRowsCount: number;
  row: CatalogPriceListRow;
}

export interface BulkUpdateCatalogPriceListRowRequest extends UpdateCatalogPriceListRowRequest {
  rowId: string;
}

export interface BulkUpdateCatalogPriceListRowsRequest {
  rows: BulkUpdateCatalogPriceListRowRequest[];
}

export interface BulkUpdateCatalogPriceListRowsResponse {
  priceListId: string;
  priceListStatus: CatalogPriceListStatus;
  rowsCount: number;
  validRowsCount: number;
  errorRowsCount: number;
  rows: CatalogPriceListRow[];
}

export interface CatalogPriceListIssueGroup {
  groupKey: string;
  issueCode: string;
  field: string;
  sourceValue: string;
  rowsCount: number;
  exampleRowNumbers: number[];
}

export interface GetCatalogPriceListIssueGroupsParams {
  priceListId: string;
  issueCode?: string | null;
  page: number;
  pageSize: number;
}

export interface GetCatalogPriceListIssueGroupsResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: CatalogPriceListIssueGroup[];
}

export interface ApplyCatalogPriceListIssueGroupRequest {
  unit?: string | null;
  productId?: string | null;
}

export interface ApplyCatalogPriceListIssueGroupResponse {
  priceListId: string;
  priceListStatus: CatalogPriceListStatus;
  processedRowsCount: number;
  rowsCount: number;
  validRowsCount: number;
  errorRowsCount: number;
}
