import { httpClient } from "@/shared/api/httpClient";
import type { SubmitCatalogImportBatchResponse } from "../model/types";

export async function submitCatalogImportBatch(
  batchId: string,
): Promise<SubmitCatalogImportBatchResponse> {
  const response =
    await httpClient.post<SubmitCatalogImportBatchResponse>(
      `/api/catalog/import-batches/${batchId}/submit`,
    );

  return response.data;
}