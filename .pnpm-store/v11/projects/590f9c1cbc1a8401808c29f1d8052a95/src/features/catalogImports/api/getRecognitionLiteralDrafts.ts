import { httpClient } from "@/shared/api/httpClient";
import type { RecognitionTrainingScope } from "../model/recognitionLiteralProposals";
import type { RecognitionLiteralDraftDetails, RecognitionLiteralDraftPage } from "../model/recognitionLiteralDrafts";

export const recognitionLiteralDraftsQueryRoot = ["recognition-literal-drafts"] as const;

export async function getRecognitionLiteralDrafts(
  scope: RecognitionTrainingScope,
  page: number,
): Promise<RecognitionLiteralDraftPage> {
  const response = await httpClient.get<RecognitionLiteralDraftPage>("/api/catalog/recognition/training/literal-drafts", { params: { ...scope, page, pageSize: 10 } });
  return response.data;
}

export async function getRecognitionLiteralDraftDetails(draftId: string): Promise<RecognitionLiteralDraftDetails> {
  const response = await httpClient.get<RecognitionLiteralDraftDetails>(`/api/catalog/recognition/training/literal-drafts/${draftId}`);
  return response.data;
}