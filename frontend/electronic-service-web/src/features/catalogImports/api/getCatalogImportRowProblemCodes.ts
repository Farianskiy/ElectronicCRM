import { httpClient } from "@/shared/api/httpClient";
import type {
  GetCatalogImportRowProblemCodesParams,
  GetCatalogImportRowProblemCodesResponse,
} from "../model/types";

export async function getCatalogImportRowProblemCodes(
  params: GetCatalogImportRowProblemCodesParams,
): Promise<GetCatalogImportRowProblemCodesResponse> {
  const response =
    await httpClient.get<GetCatalogImportRowProblemCodesResponse>(
      `/api/catalog/import-batches/${params.batchId}/rows/problem-codes`,
      {
        params: {
          expectedVersion: params.expectedVersion,
          status: params.status ?? undefined,
          search: params.search?.trim() || undefined,
        },
      },
    );

  return response.data;
}