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
  priceListId: string | null;
  priceListRowId: string | null;
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
  productTotalAmount: number;
  componentsTotalAmount: number;
  totalAmount: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  components: CatalogPriceCalculationLineComponent[];
}

export interface CatalogPriceCalculationLineComponent {
  componentLineId: string;
  needDefinitionId: string;
  needName: string;
  componentProductId: string;
  manufacturerId: string;
  manufacturerName: string;
  article: string;
  name: string;
  selectionSource: "Recommended" | "Manual";
  quantityPerUnit: number;
  totalQuantity: number;
  basePriceAmount: number;
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
  customerName?: string | null;
  objectName?: string | null;
  projectNumber?: string | null;
  responsibleName?: string | null;
  comment?: string | null;
  validUntil?: string | null;
  status: CatalogPriceCalculationStatus;
  totalAmount: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  completedAtUtc?: string | null;
  archivedAtUtc?: string | null;
  lines: CatalogPriceCalculationLine[];
  manufacturerDiscounts: CatalogPriceCalculationManufacturerDiscount[];
}

export interface UpdateCatalogPriceCalculationCardRequest {
  calculationId: string;
  customerName: string | null;
  objectName: string | null;
  projectNumber: string | null;
  responsibleName: string | null;
  comment: string | null;
  validUntil: string | null;
}

export interface UpdateCatalogPriceCalculationCardResponse {
  calculationId: string;
  customerName: string | null;
  objectName: string | null;
  projectNumber: string | null;
  responsibleName: string | null;
  comment: string | null;
  validUntil: string | null;
  updatedAtUtc: string | null;
}

export interface CatalogPriceCalculationProductSearchItem {
  productId: string;
  productTypeCode: string;
  productTypeName: string;
  manufacturerId: string;
  manufacturerName: string;
  priceStatus:
    | "Available"
    | "ActivePriceNotFound"
    | "ActivePriceAmbiguous";
  priceListId: string | null;
  priceListRowId: string | null;
  priceListEffectiveDate?: string | null;
  article: string;
  name: string;
  unit?: string | null;
  basePriceAmount: number | null;
  mrcPriceAmount?: number | null;
}

export interface SearchCatalogPriceCalculationProductsParams {
  calculationId: string;
  search: string;
  page: number;
  pageSize: number;
  productKind?: "MainProduct" | "Component";
  productTypeCode?: string;
}

export interface SearchCatalogPriceCalculationProductsResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: CatalogPriceCalculationProductSearchItem[];
}

export const catalogPriceCalculationImportRowStatuses = [
  "New",
  "QuantityChanged",
  "Removed",
  "Unchanged",
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
  existingLineId: string | null;
  currentQuantity: number | null;
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

export const catalogPriceCalculationComponentImportRowStatuses = [
  "New",
  "QuantityChanged",
  "Removed",
  "Unchanged",
  "Invalid",
  "MainLineNotFound",
  "ComponentNotFound",
  "NeedNotFound",
  "ActivePriceNotFound",
  "ActivePriceAmbiguous",
] as const;

export type CatalogPriceCalculationComponentImportRowStatus =
  (typeof catalogPriceCalculationComponentImportRowStatuses)[number];

export interface CatalogPriceCalculationComponentImportPreviewRow {
  rowNumber: number;
  mainProductArticle: string;
  needName: string;
  componentArticle: string;
  quantityPerUnit: number | null;
  mainLineId: string | null;
  existingComponentLineId: string | null;
  needDefinitionId: string | null;
  componentProductId: string | null;
  currentQuantityPerUnit: number | null;
  status: CatalogPriceCalculationComponentImportRowStatus;
  message: string | null;
}

export type CatalogPriceCalculationCharacteristicImportRowStatus =
  | "Changed"
  | "Removed"
  | "Unchanged"
  | "Invalid"
  | "ProductNotFound"
  | "CharacteristicNotFound";

export interface CatalogPriceCalculationCharacteristicImportPreviewRow {
  rowNumber: number;
  productId: string | null;
  article: string;
  productName: string;
  productTypeName: string;
  characteristicCode: string;
  characteristicName: string;
  dataType: string;
  unit: string | null;
  isRequired: boolean;
  currentValue: string | null;
  newValue: string | null;
  status: CatalogPriceCalculationCharacteristicImportRowStatus;
  message: string | null;
}

export interface PreviewCatalogPriceCalculationImportRequest {
  calculationId: string;
  file: File;
}

export interface PreviewCatalogPriceCalculationImportResponse {
  readRowsCount: number;
  matchedRowsCount: number;
  skippedRowsCount: number;
  addedRowsCount: number;
  updatedRowsCount: number;
  removedRowsCount: number;
  unchangedRowsCount: number;
  isProjectWorkbook: boolean;
  warning: string | null;
  rows: CatalogPriceCalculationImportPreviewRow[];
  componentRows: CatalogPriceCalculationComponentImportPreviewRow[];
  characteristicRows: CatalogPriceCalculationCharacteristicImportPreviewRow[];
}

export interface ApplyCatalogPriceCalculationImportRequest {
  calculationId: string;
  rows: Array<{
    action: "Add" | "UpdateQuantity" | "Remove";
    productId: string;
    existingLineId: string | null;
    quantity: number | null;
  }>;
  componentRows: Array<{
    action: "Add" | "UpdateQuantity" | "Remove";
    mainLineId: string;
    existingComponentLineId: string | null;
    needDefinitionId: string;
    componentProductId: string;
    quantityPerUnit: number | null;
  }>;
  characteristicRows: Array<{
    action: "Set" | "Remove";
    productId: string;
    characteristicCode: string;
    value: string | null;
  }>;
}

export interface ApplyCatalogPriceCalculationImportResponse {
  calculationId: string;
  addedLinesCount: number;
  updatedLinesCount: number;
  removedLinesCount: number;
  addedComponentsCount: number;
  updatedComponentsCount: number;
  removedComponentsCount: number;
  updatedCharacteristicsCount: number;
  removedCharacteristicsCount: number;
  calculationTotalAmount: number;
}
