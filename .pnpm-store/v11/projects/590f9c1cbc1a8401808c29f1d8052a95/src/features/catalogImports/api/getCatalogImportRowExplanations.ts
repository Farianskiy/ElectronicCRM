import { httpClient } from "@/shared/api/httpClient";
import type { CatalogImportProductNameExplanationSample } from "../model/types";

export interface CatalogImportRowExplanation {
  rowId: string;
  rowNumber: number;
  productName: string | null;
  status: string;
  hasConflicts: boolean;
  explanation: CatalogImportProductNameExplanationSample | null;
}

export interface CatalogImportRowExplanationsResponse {
  batchId: string;
  batchVersion: number;
  generatedAtUtc: string;
  items: CatalogImportRowExplanation[];
}

export async function getCatalogImportRowExplanations(
  batchId: string,
  expectedVersion: number,
  rowIds: readonly string[],
  signal?: AbortSignal,
): Promise<CatalogImportRowExplanationsResponse> {
  const params = new URLSearchParams({
    expectedVersion: String(expectedVersion),
  });

  rowIds.forEach((rowId) => params.append("rowIds", rowId));

  const { data } = await httpClient.get<CatalogImportRowExplanationsResponse>(
    `/api/catalog/import-batches/${batchId}/rows/name-explanations`,
    { params, signal },
  );

  const receivedIds = new Set(data.items.map((item) => item.rowId));

  if (
    data.batchId !== batchId ||
    data.batchVersion !== expectedVersion ||
    data.items.length !== rowIds.length ||
    receivedIds.size !== rowIds.length ||
    rowIds.some((rowId) => !receivedIds.has(rowId))
  ) {
    throw new Error("Ответ объяснения не соответствует запросу.");
  }

  return data;
}