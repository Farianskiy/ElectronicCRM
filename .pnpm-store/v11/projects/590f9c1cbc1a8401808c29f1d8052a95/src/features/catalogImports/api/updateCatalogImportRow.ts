import { httpClient } from "@/shared/api/httpClient";
import type {
  UpdateCatalogImportRowRequest,
  UpdateCatalogImportRowResponse,
} from "../model/types";

export async function updateCatalogImportRow(
  batchId: string,
  rowId: string,
  request: UpdateCatalogImportRowRequest,
): Promise<UpdateCatalogImportRowResponse> {
  const response =
    await httpClient.patch<UpdateCatalogImportRowResponse>(
      `/api/catalog/import-batches/${batchId}/rows/${rowId}`,
      request,
    );

  return response.data;
}