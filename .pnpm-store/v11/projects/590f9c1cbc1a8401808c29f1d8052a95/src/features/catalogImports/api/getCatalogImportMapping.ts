import { httpClient } from "@/shared/api/httpClient";
import type { GetCatalogImportMappingResponse } from "../model/types";

export async function getCatalogImportMapping(
  batchId: string,
): Promise<GetCatalogImportMappingResponse> {
  const response =
    await httpClient.get<GetCatalogImportMappingResponse>(
      `/api/catalog/import-batches/${batchId}/mapping`,
    );

  return response.data;
}