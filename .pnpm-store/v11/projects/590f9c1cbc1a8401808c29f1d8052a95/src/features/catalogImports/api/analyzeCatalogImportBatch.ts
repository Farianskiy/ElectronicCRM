import { httpClient } from "@/shared/api/httpClient";
import type { AnalyzeCatalogImportBatchResponse } from "../model/types";

export async function analyzeCatalogImportBatch(
  batchId: string,
  productTypeId?: string | null,
): Promise<AnalyzeCatalogImportBatchResponse> {
  const response =
    await httpClient.post<AnalyzeCatalogImportBatchResponse>(
      `/api/catalog/import-batches/${batchId}/analyze`,
      null,
      {
        params: productTypeId
          ? {
              productTypeId,
            }
          : undefined,
      },
    );

  return response.data;
}