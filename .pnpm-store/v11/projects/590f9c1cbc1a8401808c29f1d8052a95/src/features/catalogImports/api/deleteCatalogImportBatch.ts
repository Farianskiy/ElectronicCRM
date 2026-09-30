import { httpClient } from "@/shared/api/httpClient";

export async function deleteCatalogImportBatch(
  batchId: string,
): Promise<void> {
  await httpClient.delete(`/api/catalog/import-batches/${batchId}`);
}