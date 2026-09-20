import { httpClient } from "@/shared/api/httpClient";
import type { RecognitionTrainingScope } from "../model/recognitionLiteralProposals";
import type { RecognitionIntegerPattern } from "../model/recognitionIntegerPatterns";
import type { RecognitionIntegerBatchPreviewItem } from "../model/recognitionIntegerBatchPreview";
import type { RecognitionIntegerDraftRecheckResult } from "../model/recognitionIntegerDraftRecheck";

export interface RecognitionIntegerDraftBatchPreviewPage {
  draftId: string;
  batchId: string;
  scope: RecognitionTrainingScope;
  pattern: RecognitionIntegerPattern;
  checkedAtUtc: string;
  page: number;
  pageSize: number;
  hasMore: boolean;
  recheck: RecognitionIntegerDraftRecheckResult;
  items: RecognitionIntegerBatchPreviewItem["result"][];
}

export async function previewRecognitionIntegerDraftBatch(
  draftId: string,
  batchId: string,
  page: number,
): Promise<RecognitionIntegerDraftBatchPreviewPage> {
  const response = await httpClient.get<RecognitionIntegerDraftBatchPreviewPage>(
    `/api/catalog/recognition/training/integer-drafts/${draftId}/batches/${batchId}/preview`,
    { params: { page, pageSize: 25 } },
  );
  return response.data;
}