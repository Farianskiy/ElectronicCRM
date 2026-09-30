import { httpClient } from "@/shared/api/httpClient";
import type { RecognitionTrainingScope } from "../model/recognitionLiteralProposals";

export interface SaveRecognitionIntegerDraftRequest extends RecognitionTrainingScope {
  prefix: string;
  suffixes: string[];
  generatorVersion: string;
}

export interface SaveRecognitionIntegerDraftResponse {
  draftId: string;
}

export async function saveRecognitionIntegerDraft(request: SaveRecognitionIntegerDraftRequest): Promise<SaveRecognitionIntegerDraftResponse> {
  const response = await httpClient.post<SaveRecognitionIntegerDraftResponse>("/api/catalog/recognition/training/integer-drafts", request);
  return response.data;
}