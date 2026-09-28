export type CatalogProductTypeKind = "MainProduct" | "Component";

export interface CatalogProductTypeMetadata {
  id: string;
  code: string;
  name: string;
  kind: CatalogProductTypeKind;
}

export interface CreateCatalogProductTypeRequest {
  code: string;
  name: string;
  kind: CatalogProductTypeKind;
}

export interface CreateCatalogProductTypeResponse {
  id: string;
  code: string;
  name: string;
  kind: CatalogProductTypeKind;
}

export interface CatalogManufacturerMetadata {
  id: string;
  name: string;
}

export type CatalogCharacteristicDataType =
  | "Text"
  | "Number"
  | "Boolean";

export interface CatalogProductTypeCharacteristicMetadata {
  id: string;
  code: string;
  name: string;
  dataType: CatalogCharacteristicDataType;
  unit?: string | null;
  isRequired: boolean;
  isFilterable: boolean;
  isUsedForReplacement: boolean;
  replacementMatchMode: string;
  replacementWeight: number;
}
