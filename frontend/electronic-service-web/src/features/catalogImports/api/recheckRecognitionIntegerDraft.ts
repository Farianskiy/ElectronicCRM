import { httpClient } from "@/shared/api/httpClient";
import type { RecognitionIntegerDraftRecheckResult } from "../model/recognitionIntegerDraftRecheck";

export async function recheckRecognitionIntegerDraft(draftId: string): Promise<RecognitionIntegerDraftRecheckResult> {
  const response = await httpClient.get<RecognitionIntegerDraftRecheckResult>(
    `/api/catalog/recognition/training/integer-drafts/${draftId}/recheck`,
  );
  return response.data;
}