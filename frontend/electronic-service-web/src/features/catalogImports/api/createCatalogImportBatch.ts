import { httpClient } from "@/shared/api/httpClient";
import type { CreateCatalogImportBatchResponse } from "../model/types";

export async function createCatalogImportBatch(
  file: File,
): Promise<CreateCatalogImportBatchResponse> {
  const formData = new FormData();

  formData.append("file", file, file.name);

  const response =
    await httpClient.post<CreateCatalogImportBatchResponse>(
      "/api/catalog/import-batches",
      formData,
    );

  return response.data;
}