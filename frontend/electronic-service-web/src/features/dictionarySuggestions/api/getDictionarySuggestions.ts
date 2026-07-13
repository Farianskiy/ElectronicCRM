import { httpClient } from "@/shared/api/httpClient";
import type {
  AssistantDictionarySuggestionsResponse,
  GetDictionarySuggestionsParams,
} from "../model/types";

export async function getDictionarySuggestions(
  params: GetDictionarySuggestionsParams,
): Promise<AssistantDictionarySuggestionsResponse> {
  const queryParams = new URLSearchParams();

  if (params.status !== "All") {
    queryParams.set("status", params.status);
  }

  queryParams.set("page", params.page.toString());
  queryParams.set("pageSize", params.pageSize.toString());

  const response = await httpClient.get<AssistantDictionarySuggestionsResponse>(
    `/api/catalog/assistant/dictionary-suggestions?${queryParams.toString()}`,
  );

  return response.data;
}