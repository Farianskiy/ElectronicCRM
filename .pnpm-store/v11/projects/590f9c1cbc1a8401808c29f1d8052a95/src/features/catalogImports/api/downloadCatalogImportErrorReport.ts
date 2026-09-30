import { downloadFileResponse } from "@/shared/api/downloadFileResponse";
import { httpClient } from "@/shared/api/httpClient";

export async function downloadCatalogImportErrorReport(
  batchId: string,
  fallbackFileName: string,
): Promise<void> {
  const response =
    await httpClient.get<Blob>(
      `/api/catalog/import-batches/${batchId}/error-report`,
      {
        responseType: "blob",
      },
    );

  downloadFileResponse(
    response,
    fallbackFileName,
  );
}