import { httpClient } from "@/shared/api/httpClient";
import type { RecognitionLiteralBatchPreviewPage } from "../model/recognitionLiteralBatchPreview";

export async function previewRecognitionLiteralDraftBatch(
  draftId: string,
  batchId: string,
  page: number,
): Promise<RecognitionLiteralBatchPreviewPage> {
  const response = await httpClient.get<RecognitionLiteralBatchPreviewPage>(`/api/catalog/recognition/training/literal-drafts/${draftId}/batches/${batchId}/preview`, { params: { page, pageSize: 25 } });
  return response.data;
}