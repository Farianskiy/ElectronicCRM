import { httpClient } from "@/shared/api/httpClient";
import type {
  PreviewCatalogImportExampleFillRequest,
  PreviewCatalogImportExampleFillResponse,
} from "../model/types";

export async function previewCatalogImportExampleFill(
  batchId: string,
  request: PreviewCatalogImportExampleFillRequest,
): Promise<PreviewCatalogImportExampleFillResponse> {
  const encodedBatchId = encodeURIComponent(batchId);
  const response =
    await httpClient.post<PreviewCatalogImportExampleFillResponse>(
      `/api/catalog/import-batches/${encodedBatchId}/example-fill/preview`,
      request,
    );

  return response.data;
}
