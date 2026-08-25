export type DictionarySuggestionStatus = "Pending" | "Approved" | "Rejected";

export type DictionarySuggestionStatusFilter =
  | DictionarySuggestionStatus
  | "All";

export type DictionarySuggestionSource =
  | "Assistant"
  | "ImportRecognition"
  | "UserCorrection"
  | "RecognitionLearning"
  | "MlRecognition"
  | string;

export type DictionaryTermKind =
  | "Manufacturer"
  | "ProductType"
  | "Characteristic"
  | "SearchToken";

export interface DictionarySuggestionEvidenceExample {
  feedbackId: string;
  productName: string;
  feedbackType: string;
  suggestedRawValue: string | null;
  suggestedNormalizedValue: string | null;
  finalNormalizedValue: string | null;
  suggestedConfidence: number | null;
  suggestedSource: string | null;
  spanStart: number | null;
  spanLength: number | null;
  labelQuality: string;
  finalizedAtUtc: string | null;
}

export interface AssistantDictionarySuggestion {
  id: string;
  originalMessage: string;
  unknownPhrase: string;
  normalizedUnknownPhrase: string;
  suggestedKind: string;
  suggestedTargetCode: string | null;
  suggestedTargetValue: string;
  confidence: number;
  source: DictionarySuggestionSource;
  productTypeId: string | null;
  productTypeCode: string | null;
  productTypeName: string | null;
  characteristicDefinitionId: string | null;
  characteristicCode: string | null;
  characteristicName: string | null;
  occurrenceCount: number;
  acceptedEvidenceCount: number;
  correctedEvidenceCount: number;
  rejectedEvidenceCount: number;
  evidenceExamples: DictionarySuggestionEvidenceExample[];
  generatedAutomatically: boolean;
  approvedPhrase: string | null;
  approvedKind: string | null;
  approvedTargetCode: string | null;
  approvedTargetValue: string | null;
  approvedProductTypeId: string | null;
  approvedProductTypeCode: string | null;
  approvedProductTypeName: string | null;
  approvedCharacteristicDefinitionId: string | null;
  approvedCharacteristicCode: string | null;
  approvedCharacteristicName: string | null;
  approvedPriority: number | null;
  createdDictionaryTermId: string | null;
  status: DictionarySuggestionStatus | string;
  createdByUserId: string;
  createdAtUtc: string;
  reviewedByUserId: string | null;
  reviewedAtUtc: string | null;
  reviewComment: string | null;
}

export interface AssistantDictionarySuggestionsResponse {
  items: AssistantDictionarySuggestion[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface ApproveDictionarySuggestionRequest {
  phrase: string;
  kind: DictionaryTermKind;
  targetCode: string | null;
  targetValue: string;
  productTypeCode: string | null;
  priority: number;
  reviewComment: string | null;
}

export interface ReviewDictionarySuggestionRequest {
  reviewComment?: string | null;
}

export interface GetDictionarySuggestionsParams {
  status: DictionarySuggestionStatusFilter;
  page: number;
  pageSize: number;
}