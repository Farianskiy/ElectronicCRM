import { httpClient } from "@/shared/api/httpClient";
import type {
  CatalogImportMode,
  CreateCatalogImportBatchResponse,
} from "../model/types";

export async function createCatalogImportBatch(
  file: File,
  importMode: CatalogImportMode,
): Promise<CreateCatalogImportBatchResponse> {
  const formData = new FormData();

  formData.append("file", file, file.name);
  formData.append("importMode", importMode);

  const response =
    await httpClient.post<CreateCatalogImportBatchResponse>(
      "/api/catalog/import-batches",
      formData,
    );

  return response.data;
}
