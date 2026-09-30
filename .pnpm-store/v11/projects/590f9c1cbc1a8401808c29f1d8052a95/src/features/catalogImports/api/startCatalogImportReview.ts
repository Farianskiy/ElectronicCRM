import { httpClient } from "@/shared/api/httpClient";
import type { StartCatalogImportReviewResponse } from "../model/types";

export async function startCatalogImportReview(
  batchId: string,
): Promise<StartCatalogImportReviewResponse> {
  const response =
    await httpClient.post<StartCatalogImportReviewResponse>(
      `/api/catalog/import-batches/${batchId}/review/start`,
    );

  return response.data;
}