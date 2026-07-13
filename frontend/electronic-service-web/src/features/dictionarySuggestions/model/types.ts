export type DictionarySuggestionStatus = "Pending" | "Approved" | "Rejected";

export type DictionarySuggestionStatusFilter =
  | DictionarySuggestionStatus
  | "All";

export interface AssistantDictionarySuggestion {
  id: string;
  originalMessage: string;
  unknownPhrase: string;
  normalizedUnknownPhrase: string;
  suggestedKind: string;
  suggestedTargetCode: string | null;
  suggestedTargetValue: string;
  confidence: number;
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

export interface ReviewDictionarySuggestionRequest {
  reviewComment?: string | null;
}

export interface GetDictionarySuggestionsParams {
  status: DictionarySuggestionStatusFilter;
  page: number;
  pageSize: number;
}