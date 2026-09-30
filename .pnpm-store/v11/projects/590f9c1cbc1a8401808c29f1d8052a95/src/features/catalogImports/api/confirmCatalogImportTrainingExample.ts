import { httpClient } from "@/shared/api/httpClient";
import type { ConfirmCatalogImportTrainingExampleRequest, ConfirmCatalogImportTrainingExampleResponse } from "../model/types";

export async function confirmCatalogImportTrainingExample(
  batchId: string,
  rowId: string,
  characteristicId: string,
  request: ConfirmCatalogImportTrainingExampleRequest,
): Promise<ConfirmCatalogImportTrainingExampleResponse> {
  const response = await httpClient.post<ConfirmCatalogImportTrainingExampleResponse>(`/api/catalog/import-batches/${batchId}/rows/${rowId}/characteristics/${characteristicId}/training-example`, request);
  return response.data;
}