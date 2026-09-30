import { httpClient } from "@/shared/api/httpClient";
import type { CatalogImportFeedbackSpan } from "../model/types";

export function catalogImportConfirmedSpansQueryKey(batchId: string, rowId: string) {
  return ["catalog-import-confirmed-spans", batchId, rowId] as const;
}

export async function getCatalogImportRowConfirmedSpans(batchId: string, rowId: string): Promise<CatalogImportFeedbackSpan[]> {
  const response = await httpClient.get<CatalogImportFeedbackSpan[]>(`/api/catalog/import-batches/${batchId}/rows/${rowId}/confirmed-spans`);
  return response.data;
}