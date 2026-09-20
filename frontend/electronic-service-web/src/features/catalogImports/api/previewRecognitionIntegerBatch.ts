import { httpClient } from "@/shared/api/httpClient";
import type { RecognitionIntegerBatchPreviewPage, RecognitionIntegerBatchPreviewRequest } from "../model/recognitionIntegerBatchPreview";

export async function previewRecognitionIntegerBatch(request: RecognitionIntegerBatchPreviewRequest): Promise<RecognitionIntegerBatchPreviewPage> {
  const response = await httpClient.post<RecognitionIntegerBatchPreviewPage>("/api/catalog/recognition/training/integer-pattern-proposals/batch-preview", request);
  return response.data;
}