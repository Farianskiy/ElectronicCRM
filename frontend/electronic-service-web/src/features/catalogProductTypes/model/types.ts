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

export type ProductTypeKind = "MainProduct" | "Component";

export interface ProductTypeKindChangeBlocker {
  code: string;
  message: string;
  count: number;
}

export interface ProductTypeKindChangePreview {
  productTypeId: string;
  productTypeCode: string;
  productTypeName: string;
  currentKind: ProductTypeKind;
  targetKind: ProductTypeKind;
  productsCount: number;
  characteristicsCount: number;
  canChange: boolean;
  blockers: ProductTypeKindChangeBlocker[];
}

export interface ProductTypeArchiveBlocker {
  code: string;
  message: string;
  count: number;
}

export interface ProductTypeArchivePreview {
  productTypeId: string;
  productTypeCode: string;
  productTypeName: string;
  isArchived: boolean;
  productsCount: number;
  characteristicsCount: number;
  canArchive: boolean;
  blockers: ProductTypeArchiveBlocker[];
}
