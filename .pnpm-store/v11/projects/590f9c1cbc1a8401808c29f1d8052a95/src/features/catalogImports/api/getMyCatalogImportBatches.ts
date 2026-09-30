import { httpClient } from "@/shared/api/httpClient";
import type {
  GetMyCatalogImportBatchesParams,
  GetMyCatalogImportBatchesResponse,
} from "../model/types";

export async function getMyCatalogImportBatches(
  params: GetMyCatalogImportBatchesParams,
): Promise<GetMyCatalogImportBatchesResponse> {
  const queryParams = new URLSearchParams();

  if (params.status) {
    queryParams.set("status", params.status);
  }

  queryParams.set("page", params.page.toString());
  queryParams.set("pageSize", params.pageSize.toString());

  const response = await httpClient.get<GetMyCatalogImportBatchesResponse>(
    `/api/catalog/import-batches/my?${queryParams.toString()}`,
  );

  return response.data;
}