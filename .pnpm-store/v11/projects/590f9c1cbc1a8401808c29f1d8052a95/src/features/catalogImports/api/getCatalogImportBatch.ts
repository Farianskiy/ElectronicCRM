import { httpClient } from "@/shared/api/httpClient";
import type { CatalogImportBatchDetails } from "../model/types";

export async function getCatalogImportBatch(
  batchId: string,
): Promise<CatalogImportBatchDetails> {
  const response = await httpClient.get<CatalogImportBatchDetails>(
    `/api/catalog/import-batches/${batchId}`,
  );

  return response.data;
}