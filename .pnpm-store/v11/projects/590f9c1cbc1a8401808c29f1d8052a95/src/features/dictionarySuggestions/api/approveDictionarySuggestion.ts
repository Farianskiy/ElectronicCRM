import { httpClient } from "@/shared/api/httpClient";
import type { ApproveDictionarySuggestionRequest } from "../model/types";

export interface ApproveDictionarySuggestionParams {
  suggestionId: string;
  request: ApproveDictionarySuggestionRequest;
}

export async function approveDictionarySuggestion(
  params: ApproveDictionarySuggestionParams,
): Promise<void> {
  await httpClient.post(
    `/api/catalog/assistant/dictionary-suggestions/${params.suggestionId}/approve`,
    params.request,
  );
}