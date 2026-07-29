import { httpClient } from "@/shared/api/httpClient";
import type {
  RejectCatalogImportBatchRequest,
  RejectCatalogImportBatchResponse,
} from "../model/types";

export async function rejectCatalogImportBatch(
  batchId: string,
  request: RejectCatalogImportBatchRequest,
): Promise<RejectCatalogImportBatchResponse> {
  const response =
    await httpClient.post<RejectCatalogImportBatchResponse>(
      `/api/catalog/import-batches/${batchId}/review/reject`,
      request,
    );

  return response.data;
}