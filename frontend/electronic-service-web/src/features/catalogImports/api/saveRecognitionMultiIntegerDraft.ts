import { httpClient } from "@/shared/api/httpClient";
import type {
  RecognitionMultiIntegerPart,
  RecognitionMultiIntegerRequest,
} from "../model/recognitionMultiIntegerProposals";

export interface SaveRecognitionMultiIntegerDraftRequest
  extends RecognitionMultiIntegerRequest {
  generatorVersion: string;
  parts: RecognitionMultiIntegerPart[];
}

export interface SaveRecognitionMultiIntegerDraftResponse {
  id: string;
}

export async function saveRecognitionMultiIntegerDraft(
  request: SaveRecognitionMultiIntegerDraftRequest,
): Promise<SaveRecognitionMultiIntegerDraftResponse> {
  const response =
    await httpClient.post<SaveRecognitionMultiIntegerDraftResponse>(
      "/api/catalog/recognition/training/multi-integer-drafts",
      request,
    );

  return response.data;
}