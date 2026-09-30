export interface CatalogProductTypeCharacteristicSchemaItem {
  definitionId: string;
  code: string;
  name: string;
  dataType: string;
  unit?: string | null;

  isRequired: boolean;
  isFilterable: boolean;
  isUsedForReplacement: boolean;

  replacementMatchMode: string;
  replacementWeight: number;

  productsWithValueCount: number;
  productsWithoutValueCount: number;

  canMakeRequired: boolean;
  canRemoveFromType: boolean;
}

export interface CatalogProductTypeCharacteristicSchema {
  productTypeId: string;
  productTypeCode: string;
  productTypeName: string;
  productsCount: number;

  characteristics:
    CatalogProductTypeCharacteristicSchemaItem[];
}

export interface AvailableCharacteristicDefinition {
  id: string;
  code: string;
  name: string;
  dataType: string;
  unit?: string | null;
}