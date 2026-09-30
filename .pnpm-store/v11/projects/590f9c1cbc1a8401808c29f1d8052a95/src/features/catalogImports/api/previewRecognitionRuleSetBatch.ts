import { httpClient } from "@/shared/api/httpClient";
import type { RecognitionRuleSetNamePreviewResult } from "./previewRecognitionRuleSetName";

export interface RecognitionRuleSetFieldComparison {
  characteristicDefinitionId: string;
  name: string;
  unit: string | null;
  currentValue: string | null;
  proposedValue: string | null;
  status: string;
}

export interface RecognitionRuleSetBatchPreviewItem {
  rowId: string;
  rowNumber: number;
  productName: string | null;
  status: string;
  preview: RecognitionRuleSetNamePreviewResult | null;
    comparisons?: RecognitionRuleSetFieldComparison[];
}

export interface RecognitionRuleSetBatchPreviewPage {
  batchId: string;
  versionId: string;
  versionNumber: number;
  checkedAtUtc: string;
  page: number;
  pageSize: number;
  hasMore: boolean;
  items: RecognitionRuleSetBatchPreviewItem[];
  proposedRowsCount: number;
  conflictRowsCount: number;
  noMatchRowsCount: number;
  outsideScopeRowsCount: number;
}

export async function previewRecognitionRuleSetBatch(
  versionId: string,
  batchId: string,
  page: number,
): Promise<RecognitionRuleSetBatchPreviewPage> {
  const response =
    await httpClient.get<RecognitionRuleSetBatchPreviewPage>(
      `/api/catalog/recognition/training/rule-set-versions/${encodeURIComponent(versionId)}/preview-batch/${encodeURIComponent(batchId)}`,
      { params: { page } },
    );

  return response.data;
}