import { httpClient } from "@/shared/api/httpClient";
import type { RecognitionTrainingScope } from "../model/recognitionLiteralProposals";
import type { RecognitionIntegerPatternProposalSet } from "../model/recognitionIntegerPatterns";

export async function previewRecognitionIntegerPatterns(scope: RecognitionTrainingScope): Promise<RecognitionIntegerPatternProposalSet> {
  const response = await httpClient.get<RecognitionIntegerPatternProposalSet>("/api/catalog/recognition/training/integer-pattern-proposals", { params: scope });
  return response.data;
}