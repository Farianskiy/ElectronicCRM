import { httpClient } from "@/shared/api/httpClient";
import type {
  RequestCatalogImportChangesRequest,
  RequestCatalogImportChangesResponse,
} from "../model/types";

export async function requestCatalogImportChanges(
  batchId: string,
  request: RequestCatalogImportChangesRequest,
): Promise<RequestCatalogImportChangesResponse> {
  const response =
    await httpClient.post<RequestCatalogImportChangesResponse>(
      `/api/catalog/import-batches/${batchId}/review/request-changes`,
      request,
    );

  return response.data;
}