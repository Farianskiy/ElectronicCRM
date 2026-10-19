export type ProductNeedStatus =
  | "Unknown"
  | "Missing"
  | "Included"
  | "NotApplicable";

export type ComponentNeedRequirementMode =
  | "Optional"
  | "RequiredForAssembly";

export type ComponentCompatibilityComparisonOperator =
  | "Equal"
  | "GreaterThanOrEqual"
  | "LessThanOrEqual";

export interface CompatibleComponent {
  productId: string;
  article: string;
  name: string;
  priceAmount: number;
  priceCurrency: string;
  stockQuantity: number;
  matchConditions: ComponentCompatibilityCondition[];
}

export interface ComponentCompatibilityCondition {
  characteristicName: string;
  actualValue: string;
  expectedValues: string[];
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
  requirementMode: ComponentNeedRequirementMode;
  defaultQuantityPerUnit: number;
  status: ProductNeedStatus;
  compatibleComponents: CompatibleComponent[];
  selectedComponents: SelectedComponent[];
}

export interface ProductComponentCompatibility {
  productId: string;
  needs: ProductNeedCompatibility[];
  accessories: CompatibleComponent[];
}

export interface ComponentNeed {
  id: string;
  mainProductTypeId: string;
  mainProductTypeCode: string;
  mainProductTypeName: string;
  code: string;
  name: string;
  requirementMode: ComponentNeedRequirementMode;
  defaultQuantityPerUnit: number;
}

export interface ComponentOfferSummary {
  id: string;
  mainProductTypeId: string;
  needDefinitionId: string | null;
  needCode: string | null;
  needName: string | null;
  mainProductTypeCode: string;
  constraintsCount: number;
}

export interface ComponentCompatibilityRuleConstraint {
  characteristicDefinitionId: string;
  characteristicName: string;
  value: string;
  comparisonOperator: ComponentCompatibilityComparisonOperator;
}

export interface ComponentCompatibilityRuleGroup {
  id: string;
  position: number;
  constraints: ComponentCompatibilityRuleConstraint[];
}

export interface ComponentCompatibilityRule {
  id: string;
  componentProductId: string;
  componentArticle: string;
  componentName: string;
  mainProductTypeId: string;
  needDefinitionId: string | null;
  needCode: string | null;
  needName: string | null;
  mainProductTypeCode: string;
  mainProductTypeName: string;
  isActive: boolean;
  conditionGroups: ComponentCompatibilityRuleGroup[];
}

export interface ComponentCompatibilityRuleGroupInput {
  constraints: Array<{
    characteristicDefinitionId: string;
    value: string;
    comparisonOperator?: ComponentCompatibilityComparisonOperator;
  }>;
}

export type ComponentCompatibilityImportRowStatus =
  | "Ready"
  | "AlreadyExists"
  | "Invalid"
  | "ComponentNotFound"
  | "MainProductTypeNotFound"
  | "NeedNotFound"
  | "CharacteristicNotFound"
  | "Duplicate"
  | "WillCreate"
  | "WillUpdate"
  | "Unchanged";

export interface ComponentCompatibilityImportConstraint {
  characteristicDefinitionId: string;
  characteristicName: string;
  value: string;
  comparisonOperator: ComponentCompatibilityComparisonOperator;
}

export interface ComponentCompatibilityImportPreviewRow {
  rowNumber: number;
  componentArticle: string;
  mainProductType: string;
  need: string;
  componentProductId: string | null;
  mainProductTypeId: string | null;
  needDefinitionId: string | null;
  isActive: boolean;
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
