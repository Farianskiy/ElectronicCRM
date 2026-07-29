import { httpClient } from "@/shared/api/httpClient";
import type {
  GetCatalogImportAppliedProductsParams,
  GetCatalogImportAppliedProductsResponse,
} from "../model/types";

export async function getCatalogImportAppliedProducts(
  params: GetCatalogImportAppliedProductsParams,
): Promise<GetCatalogImportAppliedProductsResponse> {
  const response =
    await httpClient.get<GetCatalogImportAppliedProductsResponse>(
      `/api/catalog/import-batches/${params.batchId}/applied-products`,
      {
        params: {
          page: params.page,
          pageSize: params.pageSize,
        },
      },
    );

  return response.data;
}