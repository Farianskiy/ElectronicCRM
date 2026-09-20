import { httpClient } from "@/shared/api/httpClient";
import type { RecognitionLiteralDraftRecheckResult } from "../model/recognitionLiteralDraftRecheck";

export async function recheckRecognitionLiteralDraft(draftId: string): Promise<RecognitionLiteralDraftRecheckResult> {
  const response = await httpClient.get<RecognitionLiteralDraftRecheckResult>(`/api/catalog/recognition/training/literal-drafts/${draftId}/recheck`);
  return response.data;
}