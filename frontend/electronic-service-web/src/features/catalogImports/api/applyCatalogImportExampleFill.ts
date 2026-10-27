import { httpClient } from "@/shared/api/httpClient";
import type {
  ApplyCatalogImportExampleFillRequest,
  BulkUpdateCatalogImportRowsResponse,
} from "../model/types";

export async function applyCatalogImportExampleFill(
  batchId: string,
  request: ApplyCatalogImportExampleFillRequest,
): Promise<BulkUpdateCatalogImportRowsResponse> {
  const encodedBatchId = encodeURIComponent(batchId);
  const response = await httpClient.post<BulkUpdateCatalogImportRowsResponse>(
    `/api/catalog/import-batches/${encodedBatchId}/example-fill/apply`,
    request,
  );

  return response.data;
}
