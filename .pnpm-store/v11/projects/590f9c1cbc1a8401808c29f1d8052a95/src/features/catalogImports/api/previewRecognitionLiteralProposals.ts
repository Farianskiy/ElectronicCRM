import { httpClient } from "@/shared/api/httpClient";
import type { RecognitionLiteralProposalSet, RecognitionTrainingScope } from "../model/recognitionLiteralProposals";

export async function previewRecognitionLiteralProposals(scope: RecognitionTrainingScope): Promise<RecognitionLiteralProposalSet> {
  const response = await httpClient.get<RecognitionLiteralProposalSet>("/api/catalog/recognition/training/literal-proposals", { params: scope });
  return response.data;
}