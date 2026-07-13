import { httpClient } from "@/shared/api/httpClient";
import type {
  CreateDictionarySuggestionRequest,
  CreateDictionarySuggestionResponse,
} from "../model/types";

export async function createDictionarySuggestion(
  request: CreateDictionarySuggestionRequest,
): Promise<CreateDictionarySuggestionResponse> {
  const response = await httpClient.post<CreateDictionarySuggestionResponse>(
    "/api/catalog/assistant/dictionary-suggestions",
    request,
  );

  return response.data;
}