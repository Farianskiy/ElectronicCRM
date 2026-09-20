import { httpClient } from "@/shared/api/httpClient";
import type {
  PreviewCatalogAssistantBatchRequest,
  PreviewCatalogAssistantBatchResponse,
} from "../model/types";

export async function previewCatalogAssistantBatch(
  request: PreviewCatalogAssistantBatchRequest,
): Promise<PreviewCatalogAssistantBatchResponse> {
  const response = await httpClient.post<PreviewCatalogAssistantBatchResponse>(
    "/api/catalog/assistant/preview-batch",
    request,
  );

  return response.data;
}