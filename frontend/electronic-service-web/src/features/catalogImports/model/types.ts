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

export const catalogImportRecognitionShadowSampleKinds = [
  "None",
  "Conflict",
  "NotRecognized",
  "RecognitionWithoutExplicitValue",
  "Ambiguous",
] as const;

export type CatalogImportRecognitionShadowSampleKind =
  (typeof catalogImportRecognitionShadowSampleKinds)[number];

export interface CatalogImportRecognitionShadowCharacteristic {
  characteristicCode: string;
  characteristicName: string;
  explicitValuesCount: number;
  recognizedValuesCount: number;
  matchesCount: number;
  conflictsCount: number;
  notRecognizedCount: number;
  recognitionWithoutExplicitValueCount: number;
  ambiguousCount: number;
}

export interface CatalogImportRecognitionShadowConflictGroup {
  characteristicCode: string;
  characteristicName: string;
  excelValue: string;
  recognizedValue: string;
  rawRecognizedValue: string;
  recognitionSource: string;
  confidence: number;
  spanStart: number;
  spanLength: number;
  priority: number;
  recognizerKey: string;
  occurrenceCount: number;
  exampleRowNumbers: number[];
  exampleProductNames: string[];
}

export interface CatalogImportRecognitionShadowSample {
  rowNumber: number;
  productName: string;
  characteristicCode: string;
  characteristicName: string;
  kind: CatalogImportRecognitionShadowSampleKind;
  excelValue?: string | null;
  recognizedValue?: string | null;
  rawRecognizedValue?: string | null;
  confidence?: number | null;
  recognitionSource?: string | null;
  recognizerKey?: string | null;
  spanStart?: number | null;
  spanLength?: number | null;
  priority?: number | null;
  details?: string | null;
}

export interface CatalogImportRecognitionShadow {
  rowsAnalyzed: number;
  rowsWithRecognition: number;
  failedRowsCount: number;
  explicitValuesCount: number;
  recognizedValuesCount: number;
  matchesCount: number;
  conflictsCount: number;
  notRecognizedCount: number;
  recognitionWithoutExplicitValueCount: number;
  ambiguousCount: number;
  characteristics: CatalogImportRecognitionShadowCharacteristic[];
  conflictGroups: CatalogImportRecognitionShadowConflictGroup[];
  samples: CatalogImportRecognitionShadowSample[];
}

export const catalogImportManufacturerResolutionStatuses = [
  "Unresolved",
  "Resolved",
  "IgnoredNoise",
] as const;

export type CatalogImportManufacturerResolutionStatus =
  (typeof catalogImportManufacturerResolutionStatuses)[number];

export const catalogImportManufacturerResolutionSources = [
  "None",
  "ExactName",
  "ApprovedAlias",
  "IgnoredNoise",
] as const;

export type CatalogImportManufacturerResolutionSource =
  (typeof catalogImportManufacturerResolutionSources)[number];

export interface CatalogImportManufacturerResolutionGroup {
  sourceValue: string;
  normalizedSourceValue: string;
  status: CatalogImportManufacturerResolutionStatus;
  manufacturerId?: string | null;
  resolvedManufacturerName?: string | null;
  source: CatalogImportManufacturerResolutionSource;
  manufacturerAliasId?: string | null;
  manufacturerNoisePhraseId?: string | null;
  noiseReason?: string | null;
  occurrenceCount: number;
  exampleRowNumbers: number[];
  exampleProductNames: string[];
}

export interface CatalogImportManufacturerResolutionSummary {
  rowsWithManufacturerValueCount: number;
  resolvedByExactNameRowsCount: number;
  resolvedByApprovedAliasRowsCount: number;
  ignoredNoiseRowsCount: number;
  unresolvedRowsCount: number;
  groups: CatalogImportManufacturerResolutionGroup[];
}

export const catalogImportManufacturerRecognitionShadowSampleKinds = [
  "None",
  "Conflict",
  "NameConflict",
  "ComparisonUnavailable",
  "Suggestion",
  "Match",
] as const;

export type CatalogImportManufacturerRecognitionShadowSampleKind =
  (typeof catalogImportManufacturerRecognitionShadowSampleKinds)[number];

export interface CatalogImportManufacturerRecognitionShadowCandidate {
  manufacturerId: string;
  manufacturerName: string;
  rawValue: string;
  normalizedValue: string;
  confidence: number;
  source: CatalogImportManufacturerResolutionSource;
  manufacturerAliasId?: string | null;
  spanStart: number;
  spanLength: number;
}

export interface CatalogImportManufacturerRecognitionShadowSample {
  rowNumber: number;
  kind: CatalogImportManufacturerRecognitionShadowSampleKind;
  productName: string;
  excelManufacturerId?: string | null;
  excelManufacturerName?: string | null;
  excelManufacturerResolutionSource?: string | null;
  recognizedManufacturerId?: string | null;
  recognizedManufacturerName?: string | null;
  rawRecognizedValue?: string | null;
  confidence?: number | null;
  recognitionSource?: CatalogImportManufacturerResolutionSource | null;
  spanStart?: number | null;
  spanLength?: number | null;
  candidates: CatalogImportManufacturerRecognitionShadowCandidate[];
  details: string;
}

export interface CatalogImportManufacturerRecognitionShadow {
  rowsAnalyzedCount: number;
  rowsWithExcelManufacturerValueCount: number;
  rowsWithResolvedExcelManufacturerCount: number;
  rowsWithRecognizedManufacturerCount: number;
  matchesCount: number;
  conflictsCount: number;
  suggestionsCount: number;
  nameConflictsCount: number;
  nameUnresolvedCount: number;
  comparisonUnavailableCount: number;
  samplesTruncated: boolean;
  samples: CatalogImportManufacturerRecognitionShadowSample[];
}

export const catalogImportProductTypeSuggestionShadowSampleKinds = [
  "None",
  "Conflict",
  "NameConflict",
  "Suggestion",
  "Match",
  "Unresolved",
] as const;

export type CatalogImportProductTypeSuggestionShadowSampleKind =
  (typeof catalogImportProductTypeSuggestionShadowSampleKinds)[number];

export interface CatalogImportProductTypeSuggestionShadowEvidence {
  dictionaryTermId: string;
  phrase: string;
  rawValue: string;
  normalizedValue: string;
  priority: number;
  source: string;
  startIndex: number;
  length: number;
  endIndex: number;
}

export interface CatalogImportProductTypeSuggestionShadowCandidate {
  productTypeId: string;
  productTypeCode: string;
  productTypeName: string;
  highestPriority: number;
  confidence: number;
  evidence: CatalogImportProductTypeSuggestionShadowEvidence[];
}

export interface CatalogImportProductTypeSuggestionShadowSample {
  rowNumber: number;
  kind: CatalogImportProductTypeSuggestionShadowSampleKind;
  productName: string;
  selectedProductTypeId?: string | null;
  selectedProductTypeCode?: string | null;
  selectedProductTypeName?: string | null;
  suggestedProductTypeId?: string | null;
  suggestedProductTypeCode?: string | null;
  suggestedProductTypeName?: string | null;
  confidence?: number | null;
  highestPriority?: number | null;
  candidates: CatalogImportProductTypeSuggestionShadowCandidate[];
  details: string;
}

export interface CatalogImportProductTypeSuggestionShadowGroup {
  productTypeId: string;
  productTypeCode: string;
  productTypeName: string;
  rowsCount: number;
  matchesCount: number;
  conflictsCount: number;
  suggestionsCount: number;
  highestConfidence: number;
  exampleRowNumbers: number[];
}

export interface CatalogImportProductTypeSuggestionShadow {
  rowsAnalyzedCount: number;
  hasSelectedProductType: boolean;
  selectedProductTypeId?: string | null;
  selectedProductTypeCode?: string | null;
  selectedProductTypeName?: string | null;
  suggestedRowsCount: number;
  matchesCount: number;
  conflictsCount: number;
  suggestionsCount: number;
  nameConflictsCount: number;
  unresolvedCount: number;
  distinctSuggestedProductTypesCount: number;
  hasMixedSuggestedProductTypes: boolean;
  samplesTruncated: boolean;
  typeGroups: CatalogImportProductTypeSuggestionShadowGroup[];
  samples: CatalogImportProductTypeSuggestionShadowSample[];
}

export const catalogImportProductNameExplanationSampleKinds = [
  "None",
  "Unexplained",
  "PartiallyExplained",
  "FullyExplained",
] as const;

export type CatalogImportProductNameExplanationSampleKind =
  (typeof catalogImportProductNameExplanationSampleKinds)[number];

export const catalogImportProductNameEvidenceKinds = [
  "None",
  "Manufacturer",
  "ProductType",
  "Characteristic",
] as const;

export type CatalogImportProductNameEvidenceKind =
  (typeof catalogImportProductNameEvidenceKinds)[number];

export interface CatalogImportProductNameEvidenceSpan {
  kind: CatalogImportProductNameEvidenceKind;
  targetCode: string;
  targetValue: string;
  rawValue: string;
  source: string;
  confidence: number;
  priority: number;
  startIndex: number;
  length: number;
  endIndex: number;
}

export interface CatalogImportProductNameUnexplainedSpan {
  rawValue: string;
  startIndex: number;
  length: number;
  endIndex: number;
}

export interface CatalogImportProductNameExplanationSample {
  rowNumber: number;
  kind: CatalogImportProductNameExplanationSampleKind;
  productName: string;
  meaningfulCharactersCount: number;
  coveredMeaningfulCharactersCount: number;
  coverage: number;
  isFullyExplained: boolean;
  hasUnexplainedSpans: boolean;
  manufacturerEvidenceCount: number;
  productTypeEvidenceCount: number;
  characteristicEvidenceCount: number;
  evidence: CatalogImportProductNameEvidenceSpan[];
  unexplainedSpans: CatalogImportProductNameUnexplainedSpan[];
}

export interface CatalogImportProductNameExplanation {
  rowsAnalyzedCount: number;
  rowsWithEvidenceCount: number;
  fullyExplainedRowsCount: number;
  partiallyExplainedRowsCount: number;
  unexplainedRowsCount: number;
  averageCoverage: number;
  samplesTruncated: boolean;
  samples: CatalogImportProductNameExplanationSample[];
}


export interface CatalogImportRecognitionAppliedValue {
  rowNumber: number;
  characteristicDefinitionId: string;
  characteristicCode: string;
  characteristicName: string;
  value: string;
  rawValue: string;
  recognitionSource: string;
  confidence: number;
  spanStart: number;
  spanLength: number;
  priority: number;
  recognizerKey: string;
}

export interface CatalogImportRecognitionEnrichment {
  rowsAnalyzedCount: number;
  filledRowsCount: number;
  filledValuesCount: number;
  blockedByRecognitionConflictCount: number;
  blockedByLowConfidenceCount: number;
  blockedByUnsupportedSourceCount: number;
  blockedByInvalidExcelValueCount: number;
  blockedByInvalidRecognizedValueCount: number;
  failedRecognitionRowsCount: number;
  appliedValuesDetailsTruncated: boolean;
  appliedValues: CatalogImportRecognitionAppliedValue[];
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
  manufacturerResolutionSummary: CatalogImportManufacturerResolutionSummary;
  manufacturerRecognitionShadow: CatalogImportManufacturerRecognitionShadow;
  productTypeSuggestionShadow: CatalogImportProductTypeSuggestionShadow;
  recognitionShadow?: CatalogImportRecognitionShadow | null;
  productNameExplanation: CatalogImportProductNameExplanation;
  recognitionEnrichment: CatalogImportRecognitionEnrichment;
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

export type CatalogImportRowProblemKind = "Error" | "Warning";

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
  productTypeId?: string | null;
  productTypeResolutionSource?: string | null;
  productTypeResolutionConfidence?: number | null;
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
  search?: string | null;
  issueCode?: string | null;
  problemKind?: CatalogImportRowProblemKind | null;
  manufacturerGroupKey?: string | null;
  page: number;
  pageSize: number;
}

export type CatalogImportManufacturerGroupResolutionSource =
  | "ExactName"
  | "ApprovedAlias"
  | "IgnoredNoise"
  | "Unresolved"
  | "Manual"
  | "Mixed";

export interface CatalogImportManufacturerGroup {
  groupKey: string;
  sourceValue: string;
  resolutionSource: CatalogImportManufacturerGroupResolutionSource;
  resolvedManufacturerName?: string | null;
  exactNameRowsCount: number;
  approvedAliasRowsCount: number;
  ignoredNoiseRowsCount: number;
  unresolvedRowsCount: number;
  manualRowsCount: number;
  rowsCount: number;
}

export interface GetCatalogImportManufacturerGroupsResponse {
  batchId: string;
  batchVersion: number;
  items: CatalogImportManufacturerGroup[];
}

export interface GetCatalogImportManufacturerGroupsParams {
  batchId: string;
  expectedVersion: number;
}

export interface CatalogImportRowProblemCode {
  code: string;
  rowsCount: number;
  errorRowsCount: number;
  warningRowsCount: number;
}

export interface GetCatalogImportRowProblemCodesResponse {
  batchId: string;
  batchVersion: number;
  totalRowsCount: number;
  errorRowsCount: number;
  warningRowsCount: number;
  items: CatalogImportRowProblemCode[];
}

export interface GetCatalogImportRowProblemCodesParams {
  batchId: string;
  expectedVersion: number;
  status?: CatalogImportRowFilterStatus | null;
  search?: string | null;
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
  productTypeId: string | null;
  columns: UpdateCatalogImportColumnMappingRequest[];
}

export interface UpdateCatalogImportMappingResponse {
  batchId: string;
  status: CatalogImportBatchStatus;
  productTypeId: string | null;
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

export interface BulkUpdateCatalogImportRowRequest {
  rowId: string;
  name: string | null;
  article: string | null;
  manufacturerId: string | null;
  price: number | null;
  stockQuantity: number | null;
  characteristics: Record<string, string>;
}

export interface BulkUpdateCatalogImportRowsRequest {
  expectedVersion: number;
  rows: BulkUpdateCatalogImportRowRequest[];
}

export interface BulkUpdatedCatalogImportRowResponse {
  rowId: string;
  rowNumber: number;
  rowStatus: CatalogImportRowStatus;
  data: CatalogImportNormalizedRow;
  issues: CatalogImportRowIssue[];
  warnings: CatalogImportRowIssue[];
}

export interface BulkUpdateCatalogImportRowsResponse {
  rows: BulkUpdatedCatalogImportRowResponse[];
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

