import { httpClient } from "@/shared/api/httpClient";
import type {
  GetCatalogImportRowsParams,
  GetCatalogImportRowsResponse,
} from "../model/types";

export async function getCatalogImportRows(
  params: GetCatalogImportRowsParams,
): Promise<GetCatalogImportRowsResponse> {
  const response =
    await httpClient.get<GetCatalogImportRowsResponse>(
      `/api/catalog/import-batches/${params.batchId}/rows`,
      {
        params: {
          status: params.status ?? undefined,
          search: params.search?.trim() || undefined,
          issueCode: params.issueCode ?? undefined,
          problemKind: params.problemKind ?? undefined,
          manufacturerGroupKey: params.manufacturerGroupKey ?? undefined,
          page: params.page,
          pageSize: params.pageSize,
        },
      },
    );

  return response.data;
}