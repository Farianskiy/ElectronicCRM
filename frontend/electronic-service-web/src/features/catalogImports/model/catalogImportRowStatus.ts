import type {
  CatalogImportRowFilterStatus,
  CatalogImportRowStatus,
} from "./types";

const statusLabels: Record<CatalogImportRowStatus, string> = {
  None: "Не определён",
  PendingMapping: "Ожидает сопоставления",
  Valid: "Корректная",
  Error: "С ошибками",
};

export function getCatalogImportRowStatusLabel(
  status: CatalogImportRowStatus,
): string {
  return statusLabels[status];
}

export function getCatalogImportRowFilterStatusLabel(
  status: CatalogImportRowFilterStatus,
): string {
  return statusLabels[status];
}