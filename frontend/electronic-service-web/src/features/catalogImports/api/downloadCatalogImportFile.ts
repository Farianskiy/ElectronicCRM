import { httpClient } from "@/shared/api/httpClient";

function getDownloadFileName(
  contentDisposition: string | undefined,
  fallbackFileName: string,
): string {
  if (!contentDisposition) {
    return fallbackFileName;
  }

  const encodedFileNameMatch = contentDisposition.match(
    /filename\*=UTF-8''([^;]+)/i,
  );

  if (encodedFileNameMatch?.[1]) {
    try {
      return decodeURIComponent(encodedFileNameMatch[1]);
    } catch {
      return fallbackFileName;
    }
  }

  const regularFileNameMatch = contentDisposition.match(
    /filename="?([^";]+)"?/i,
  );

  return regularFileNameMatch?.[1] ?? fallbackFileName;
}

export async function downloadCatalogImportFile(
  batchId: string,
  fallbackFileName: string,
): Promise<void> {
  const response = await httpClient.get<Blob>(
    `/api/catalog/import-batches/${batchId}/file`,
    {
      responseType: "blob",
    },
  );

  const contentDisposition = response.headers["content-disposition"];

  const fileName = getDownloadFileName(
    typeof contentDisposition === "string" ? contentDisposition : undefined,
    fallbackFileName,
  );

  const objectUrl = URL.createObjectURL(response.data);
  const link = document.createElement("a");

  link.href = objectUrl;
  link.download = fileName;
  link.style.display = "none";

  document.body.appendChild(link);
  link.click();
  link.remove();

  window.setTimeout(() => {
    URL.revokeObjectURL(objectUrl);
  }, 0);
}