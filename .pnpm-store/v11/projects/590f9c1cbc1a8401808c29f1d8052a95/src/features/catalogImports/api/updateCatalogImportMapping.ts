import { httpClient } from "@/shared/api/httpClient";
import type {
  UpdateCatalogImportMappingRequest,
  UpdateCatalogImportMappingResponse,
} from "../model/types";

export async function updateCatalogImportMapping(
  batchId: string,
  request: UpdateCatalogImportMappingRequest,
): Promise<UpdateCatalogImportMappingResponse> {
  const response =
    await httpClient.put<UpdateCatalogImportMappingResponse>(
      `/api/catalog/import-batches/${batchId}/mapping`,
      request,
    );

  return response.data;
}