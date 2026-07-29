import { httpClient } from "@/shared/api/httpClient";
import type {
  GetCatalogImportReviewQueueParams,
  GetCatalogImportReviewQueueResponse,
} from "../model/types";

export async function getCatalogImportReviewQueue(
  params: GetCatalogImportReviewQueueParams,
): Promise<GetCatalogImportReviewQueueResponse> {
  const response =
    await httpClient.get<GetCatalogImportReviewQueueResponse>(
      "/api/catalog/import-batches/review-queue",
      {
        params: {
          status: params.status ?? undefined,
          page: params.page,
          pageSize: params.pageSize,
        },
      },
    );

  return response.data;
}