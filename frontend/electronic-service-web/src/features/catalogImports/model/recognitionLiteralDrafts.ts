export interface RecognitionLiteralDraftListItem {
  id: string;
  manufacturerId: string;
  productTypeId: string;
  characteristicDefinitionId: string;
  literal: string;
  normalizedValue: string;
  generatorVersion: string;
  matchedNameCount: number;
  checkedExampleCount: number;
  supportingExampleCount: number;
  createdAtUtc: string;
}

export interface RecognitionLiteralDraftPage {
  items: RecognitionLiteralDraftListItem[];
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export interface RecognitionLiteralDraftEvidence {
  exampleId: string;
  productName: string;
  rawValue: string;
  normalizedValue: string;
  spanStart: number;
  spanLength: number;
  isSupporting: boolean;
  confirmedAtUtc: string;
  revokedAtUtc: string | null;
}

export interface RecognitionLiteralDraftDetails {
  draft: RecognitionLiteralDraftListItem;
  evidence: RecognitionLiteralDraftEvidence[];
}