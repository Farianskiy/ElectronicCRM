import { httpClient } from "@/shared/api/httpClient";
import type { ReviewDictionarySuggestionRequest } from "../model/types";

export interface RejectDictionarySuggestionParams {
  suggestionId: string;
  request: ReviewDictionarySuggestionRequest;
}

export async function rejectDictionarySuggestion(
  params: RejectDictionarySuggestionParams,
): Promise<void> {
  await httpClient.post(
    `/api/catalog/assistant/dictionary-suggestions/${params.suggestionId}/reject`,
    params.request,
  );
}