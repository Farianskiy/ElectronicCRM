import { httpClient } from "@/shared/api/httpClient";
import type { RecognitionTrainingScope } from "../model/recognitionLiteralProposals";

export interface SaveRecognitionLiteralDraftRequest extends RecognitionTrainingScope {
  literal: string;
  normalizedValue: string;
  generatorVersion: string;
}

export interface SaveRecognitionLiteralDraftResponse {
  draftId: string;
}

export async function saveRecognitionLiteralDraft(request: SaveRecognitionLiteralDraftRequest): Promise<SaveRecognitionLiteralDraftResponse> {
  const response = await httpClient.post<SaveRecognitionLiteralDraftResponse>("/api/catalog/recognition/training/literal-drafts", request);
  return response.data;
}