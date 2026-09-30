import { httpClient } from "@/shared/api/httpClient";
import type {
  BulkUpdateCatalogImportRowsRequest,
  BulkUpdateCatalogImportRowsResponse,
} from "../model/types";

export async function bulkUpdateCatalogImportRows(
  batchId: string,
  request: BulkUpdateCatalogImportRowsRequest,
): Promise<BulkUpdateCatalogImportRowsResponse> {
  const encodedBatchId = encodeURIComponent(batchId);

  const response =
    await httpClient.put<BulkUpdateCatalogImportRowsResponse>(
      `/api/catalog/import-batches/${encodedBatchId}/rows/bulk`,
      request,
    );

  return response.data;
}