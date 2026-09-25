import { httpClient } from "@/shared/api/httpClient";
import type { ApproveDictionarySuggestionRequest } from "../model/types";

export async function evaluateDictionarySuggestion(id: string, request: ApproveDictionarySuggestionRequest) {
  return (await httpClient.post<{ reportId: string }>(`/api/catalog/assistant/dictionary-suggestions/${id}/evaluations`, request)).data;
}
