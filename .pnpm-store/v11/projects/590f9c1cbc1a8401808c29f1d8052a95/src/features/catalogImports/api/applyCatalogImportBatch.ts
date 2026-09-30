import { httpClient } from "@/shared/api/httpClient";
import type { ApplyCatalogImportBatchResponse } from "../model/types";

export async function applyCatalogImportBatch(
  batchId: string,
): Promise<ApplyCatalogImportBatchResponse> {
  const response =
    await httpClient.post<ApplyCatalogImportBatchResponse>(
      `/api/catalog/import-batches/${batchId}/apply`,
    );

  return response.data;
}