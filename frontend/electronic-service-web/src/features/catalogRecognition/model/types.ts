export type CatalogRecognitionSource =
  | "None"
  | "Dictionary"
  | "Rule"
  | "Heuristic"
  | "MachineLearning";

export type CatalogRecognitionStrategyKind =
  | "NumericWithUnit"
  | "PoleCount"
  | "EnumToken"
  | "BooleanAlias"
  | "Dimensions"
  | "Dictionary";

export interface CatalogRecognizedCharacteristic {
  characteristicCode: string;
  rawValue: string;
  normalizedValue: string;
  confidence: number;
  source: CatalogRecognitionSource;
  startIndex: number;
  length: number;
  priority: number;
  recognizerKey: string;
}

export interface CatalogRecognitionConflict {
  characteristicCode: string;
  candidates: CatalogRecognizedCharacteristic[];
}

export interface CatalogCharacteristicRecognitionProfile {
  id: string;
  productTypeId: string;
  characteristicDefinitionId: string;
  characteristicCode: string;
  characteristicName: string;
  strategyKind: CatalogRecognitionStrategyKind;
  priority: number;
  minimumConfidence: number;
  configurationJson: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CatalogProductNameRecognitionPreview {
  hasProductTypeScope: boolean;
  productTypeId: string | null;
  productTypeCode: string | null;
  productTypeName: string | null;
  allowedCharacteristicCodes: string[] | null;
  recognitionProfiles: CatalogCharacteristicRecognitionProfile[];
  productName: string;
  normalizedProductName: string;
  characteristics: CatalogRecognizedCharacteristic[];
  conflicts: CatalogRecognitionConflict[];
  candidates: CatalogRecognizedCharacteristic[];
}

export interface PreviewCatalogProductNameRecognitionParameters {
  productName: string;
  productTypeCode: string | null;
}

export interface CreateCatalogCharacteristicRecognitionProfileParameters {
  productTypeCode: string;
  characteristicDefinitionId: string;
  strategyKind: CatalogRecognitionStrategyKind;
  priority: number;
  minimumConfidence: number;
  configurationJson: string;
}

export interface CreateCatalogCharacteristicRecognitionProfileResult {
  profileId: string;
}

export interface UpdateCatalogCharacteristicRecognitionProfileParameters {
  profileId: string;
  strategyKind: CatalogRecognitionStrategyKind;
  priority: number;
  minimumConfidence: number;
  configurationJson: string;
}

export interface SetCatalogCharacteristicRecognitionProfileActiveParameters {
  profileId: string;
  isActive: boolean;
}