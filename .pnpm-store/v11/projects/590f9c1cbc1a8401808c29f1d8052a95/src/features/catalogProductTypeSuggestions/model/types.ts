export type CatalogProductTypeSuggestionStatus =
  | "None"
  | "Unresolved"
  | "Suggested"
  | "Conflict";

export interface CatalogProductTypeSuggestionEvidence {
  dictionaryTermId: string;
  phrase: string;
  rawValue: string;
  normalizedValue: string;
  priority: number;
  source: string;
  startIndex: number;
  length: number;
  endIndex: number;
}

export interface CatalogProductTypeSuggestionCandidate {
  productTypeId: string;
  productTypeCode: string;
  productTypeName: string;
  highestPriority: number;
  confidence: number;
  evidence: CatalogProductTypeSuggestionEvidence[];
}

export interface CatalogProductTypeSuggestionPreview {
  productName: string;
  normalizedProductName: string;
  status: CatalogProductTypeSuggestionStatus;
  isSuggested: boolean;
  isConflict: boolean;
  selectedCandidate: CatalogProductTypeSuggestionCandidate | null;
  candidates: CatalogProductTypeSuggestionCandidate[];
}

export interface PreviewCatalogProductTypeSuggestionParameters {
  productName: string;
}