import { httpClient } from "@/shared/api/httpClient";
import type {
  AskCatalogAssistantRequest,
  AskCatalogAssistantResponse,
} from "../model/types";

export async function askCatalogAssistant(
  request: AskCatalogAssistantRequest,
): Promise<AskCatalogAssistantResponse> {
  const response = await httpClient.post<AskCatalogAssistantResponse>(
    "/api/catalog/assistant/ask",
    request,
  );

  return response.data;
}