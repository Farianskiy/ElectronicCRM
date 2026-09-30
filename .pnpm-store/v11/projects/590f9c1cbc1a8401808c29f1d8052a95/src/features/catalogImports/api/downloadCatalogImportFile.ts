import { downloadFileResponse } from "@/shared/api/downloadFileResponse";
import { httpClient } from "@/shared/api/httpClient";

export async function downloadCatalogImportFile(
  batchId: string,
  fallbackFileName: string,
): Promise<void> {
  const response =
    await httpClient.get<Blob>(
      `/api/catalog/import-batches/${batchId}/file`,
      {
        responseType: "blob",
      },
    );

  downloadFileResponse(
    response,
    fallbackFileName,
  );
}