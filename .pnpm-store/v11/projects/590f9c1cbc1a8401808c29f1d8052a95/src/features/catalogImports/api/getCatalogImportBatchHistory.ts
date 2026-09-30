import { httpClient } from "@/shared/api/httpClient";
import type { GetCatalogImportBatchHistoryResponse } from "../model/types";

export async function getCatalogImportBatchHistory(
  batchId: string,
): Promise<GetCatalogImportBatchHistoryResponse> {
  const response =
    await httpClient.get<GetCatalogImportBatchHistoryResponse>(
      `/api/catalog/import-batches/${batchId}/history`,
    );

  return response.data;
}