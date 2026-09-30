export type CatalogCharacteristicDataType =
  | "Text"
  | "Number"
  | "Boolean";

export interface CatalogCharacteristicDefinition {
  id: string;
  code: string;
  name: string;
  dataType: CatalogCharacteristicDataType;
  unit?: string | null;
  productTypesCount: number;
  productsWithValueCount: number;
}

export interface CreateCharacteristicDefinitionRequest {
  code: string;
  name: string;
  dataType: CatalogCharacteristicDataType;
  unit?: string | null;
}

export interface CreateCharacteristicDefinitionResponse {
  id: string;
}

export interface UpdateCharacteristicDefinitionRequest {
  name: string;
  unit?: string | null;
}