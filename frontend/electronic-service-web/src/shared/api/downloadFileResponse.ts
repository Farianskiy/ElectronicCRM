import type { AxiosResponse } from "axios";

function getDownloadFileName(
  contentDisposition: string | undefined,
  fallbackFileName: string,
): string {
  if (!contentDisposition) {
    return fallbackFileName;
  }

  const encodedFileNameMatch =
    contentDisposition.match(
      /filename\*=UTF-8''([^;]+)/i,
    );

  if (encodedFileNameMatch?.[1]) {
    try {
      return decodeURIComponent(
        encodedFileNameMatch[1],
      );
    } catch {
      return fallbackFileName;
    }
  }

  const regularFileNameMatch =
    contentDisposition.match(
      /filename="?([^";]+)"?/i,
    );

  return (
    regularFileNameMatch?.[1] ??
    fallbackFileName
  );
}

export function downloadFileResponse(
  response: AxiosResponse<Blob>,
  fallbackFileName: string,
): void {
  const contentDisposition =
    response.headers["content-disposition"];

  const fileName = getDownloadFileName(
    typeof contentDisposition === "string"
      ? contentDisposition
      : undefined,
    fallbackFileName,
  );

  const objectUrl =
    URL.createObjectURL(response.data);

  const link =
    document.createElement("a");

  link.href = objectUrl;
  link.download = fileName;
  link.style.display = "none";

  document.body.appendChild(link);

  link.click();
  link.remove();

  window.setTimeout(() => {
    URL.revokeObjectURL(objectUrl);
  }, 1000);
}