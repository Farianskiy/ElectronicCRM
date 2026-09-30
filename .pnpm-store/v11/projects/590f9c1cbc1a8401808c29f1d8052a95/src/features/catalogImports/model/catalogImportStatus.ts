import type { CatalogImportBatchStatus } from "./types";

const statusLabels: Record<CatalogImportBatchStatus, string> = {
  Uploaded: "Файл загружен",
  MappingRequired: "Требуется сопоставление",
  NeedsCorrection: "Требуется исправление",
  Ready: "Готов к отправке",
  Submitted: "Отправлен на проверку",
  UnderReview: "На проверке",
  Applying: "Применяется",
  Applied: "Применён",
  Rejected: "Отклонён",
  Failed: "Ошибка",
  ChangesRequested: "Возвращён на исправление",
};

export function getCatalogImportStatusLabel(
  status: CatalogImportBatchStatus,
): string {
  return statusLabels[status];
}