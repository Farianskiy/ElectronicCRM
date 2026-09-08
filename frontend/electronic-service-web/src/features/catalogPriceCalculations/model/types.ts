export const catalogPriceCalculationStatuses = [
  "Draft",
  "Completed",
  "Archived",
] as const;

export type CatalogPriceCalculationStatus =
  (typeof catalogPriceCalculationStatuses)[number];

export interface CatalogPriceCalculationListItem {
  calculationId: string;
  title: string;
  currency: string;
  status: CatalogPriceCalculationStatus;
  totalAmount: number;
  linesCount: number;
  manufacturersCount: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  completedAtUtc?: string | null;
  archivedAtUtc?: string | null;
}

export interface GetMyCatalogPriceCalculationsParams {
  status?: CatalogPriceCalculationStatus | null;
  page: number;
  pageSize: number;
}

export interface GetMyCatalogPriceCalculationsResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: CatalogPriceCalculationListItem[];
}

export interface CreateCatalogPriceCalculationRequest {
  title: string;
}

export interface CreateCatalogPriceCalculationResponse {
  calculationId: string;
  createdByUserId: string;
  title: string;
  currency: string;
  status: CatalogPriceCalculationStatus;
  totalAmount: number;
  createdAtUtc: string;
}

export interface CatalogPriceCalculationLine {
  lineId: string;
  productId: string;
  manufacturerId: string;
  manufacturerName: string;
  priceListId: string;
  priceListRowId: string;
  article: string;
  name: string;
  unit?: string | null;
  quantity: number;
  stockQuantity: number;
  shortageQuantity: number;
  basePriceAmount: number;
  mrcPriceAmount?: number | null;
  discountPercent: number;
  projectPriceAmount: number;
  totalAmount: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface CatalogPriceCalculationManufacturerDiscount {
  discountId: string;
  manufacturerId: string;
  manufacturerName: string;
  discountPercent: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface CatalogPriceCalculationDetails {
  calculationId: string;
  createdByUserId: string;
  title: string;
  currency: string;
  status: CatalogPriceCalculationStatus;
  totalAmount: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  completedAtUtc?: string | null;
  archivedAtUtc?: string | null;
  lines: CatalogPriceCalculationLine[];
  manufacturerDiscounts: CatalogPriceCalculationManufacturerDiscount[];
}

export interface CatalogPriceCalculationProductSearchItem {
  productId: string;
  manufacturerId: string;
  manufacturerName: string;
  priceListId: string;
  priceListRowId: string;
  priceListEffectiveDate?: string | null;
  article: string;
  name: string;
  unit?: string | null;
  basePriceAmount: number;
  mrcPriceAmount?: number | null;
}

export interface SearchCatalogPriceCalculationProductsParams {
  calculationId: string;
  search: string;
  page: number;
  pageSize: number;
}

export interface SearchCatalogPriceCalculationProductsResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: CatalogPriceCalculationProductSearchItem[];
}

export const catalogPriceCalculationImportRowStatuses = [
  "Matched",
  "Invalid",
  "ProductNotFound",
  "ProductAmbiguous",
  "ActivePriceNotFound",
  "ActivePriceAmbiguous",
] as const;

export type CatalogPriceCalculationImportRowStatus =
  (typeof catalogPriceCalculationImportRowStatuses)[number];

export interface CatalogPriceCalculationImportPreviewRow {
  rowNumber: number;
  article: string;
  sourceName: string | null;
  sourceManufacturer: string | null;
  quantity: number | null;
  status: CatalogPriceCalculationImportRowStatus;
  message: string | null;
  productId: string | null;
  productArticle: string | null;
  productName: string | null;
  manufacturerId: string | null;
  manufacturerName: string | null;
  stockQuantity: number | null;
  shortageQuantity: number | null;
  priceListId: string | null;
  priceListRowId: string | null;
  basePriceAmount: number | null;
  mrcPriceAmount: number | null;
}

export interface PreviewCatalogPriceCalculationImportRequest {
  calculationId: string;
  file: File;
}

export interface PreviewCatalogPriceCalculationImportResponse {
  readRowsCount: number;
  matchedRowsCount: number;
  skippedRowsCount: number;
  rows: CatalogPriceCalculationImportPreviewRow[];
}

export interface ApplyCatalogPriceCalculationImportRequest {
  calculationId: string;
  rows: Array<{
    productId: string;
    quantity: number;
  }>;
}

export interface ApplyCatalogPriceCalculationImportResponse {
  calculationId: string;
  addedLinesCount: number;
  calculationTotalAmount: number;
}
