import { httpClient } from "@/shared/api/httpClient";

export async function revokeCatalogImportTrainingExample(
  batchId: string,
  rowId: string,
  exampleId: string,
): Promise<void> {
  await httpClient.post(`/api/catalog/import-batches/${batchId}/rows/${rowId}/training-examples/${exampleId}/revoke`);
}