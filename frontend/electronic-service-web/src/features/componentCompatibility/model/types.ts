export type ProductNeedStatus =
  | "Unknown"
  | "Missing"
  | "Included"
  | "NotApplicable";

export interface CompatibleComponent {
  productId: string;
  article: string;
  name: string;
  priceAmount: number;
  priceCurrency: string;
}

export interface SelectedComponent {
  id: string;
  needDefinitionId: string;
  componentProductId: string;
  article: string;
  name: string;
  quantity: number;
  priceAmount: number;
  priceCurrency: string;
  lineTotalAmount: number;
}

export interface ProductNeedCompatibility {
  needDefinitionId: string;
  code: string;
  name: string;
  status: ProductNeedStatus;
  compatibleComponents: CompatibleComponent[];
  selectedComponents: SelectedComponent[];
}

export interface ProductComponentCompatibility {
  productId: string;
  needs: ProductNeedCompatibility[];
}

export interface ComponentNeed {
  id: string;
  mainProductTypeId: string;
  mainProductTypeCode: string;
  mainProductTypeName: string;
  code: string;
  name: string;
}

export interface ComponentOfferSummary {
  id: string;
  needDefinitionId: string;
  needCode: string;
  needName: string;
  mainProductTypeCode: string;
  constraintsCount: number;
}

export type ComponentCompatibilityImportRowStatus =
  | "Ready"
  | "AlreadyExists"
  | "Invalid"
  | "ComponentNotFound"
  | "MainProductTypeNotFound"
  | "NeedNotFound"
  | "CharacteristicNotFound"
  | "Duplicate";

export interface ComponentCompatibilityImportConstraint {
  characteristicDefinitionId: string;
  characteristicName: string;
  value: string;
}

export interface ComponentCompatibilityImportPreviewRow {
  rowNumber: number;
  componentArticle: string;
  mainProductType: string;
  need: string;
  componentProductId: string | null;
  needDefinitionId: string | null;
  status: ComponentCompatibilityImportRowStatus;
  message: string | null;
  constraints: ComponentCompatibilityImportConstraint[];
}

export interface PreviewComponentCompatibilityImportResponse {
  readRowsCount: number;
  readyRowsCount: number;
  skippedRowsCount: number;
  rows: ComponentCompatibilityImportPreviewRow[];
}

export interface ApplyComponentCompatibilityImportResponse {
  createdRulesCount: number;
}
