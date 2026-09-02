export type CatalogDictionaryTermKind =
  | "Manufacturer"
  | "ProductType"
  | "Characteristic"
  | "SearchToken";

export type CatalogDictionaryTermStatus =
  | "Pending"
  | "Approved"
  | "Rejected"
  | "Disabled";

export interface AddCatalogDictionaryTermParameters {
  productTypeCode: string | null;
  phrase: string;
  kind: CatalogDictionaryTermKind;
  targetCode: string | null;
  targetValue: string;
  priority: number;
}

export interface AddCatalogDictionaryTermResult {
  id: string;
  productTypeId: string | null;
  productTypeCode: string | null;
  phrase: string;
  normalizedPhrase: string;
  kind: CatalogDictionaryTermKind;
  targetCode: string | null;
  targetValue: string;
  priority: number;
  status: string;
  source: string;
}

export interface CatalogDictionaryTerm {
  id: string;
  productTypeId: string | null;
  phrase: string;
  normalizedPhrase: string;
  kind: CatalogDictionaryTermKind;
  targetCode: string | null;
  targetValue: string;
  priority: number;
  status: CatalogDictionaryTermStatus;
  source: string;
  createdAtUtc: string;
  approvedAtUtc: string | null;
  disabledAtUtc: string | null;
  disabledByUserId: string | null;
  disabledByUserDisplayName: string | null;
  disableReason: string | null;
  reactivatedAtUtc: string | null;
  reactivatedByUserId: string | null;
  reactivatedByUserDisplayName: string | null;
}

export interface SetCatalogDictionaryTermActiveParameters {
  termId: string;
  isActive: boolean;
  reason: string | null;
}