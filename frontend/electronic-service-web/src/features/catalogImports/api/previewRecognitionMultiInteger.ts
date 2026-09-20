import { httpClient } from "@/shared/api/httpClient";
import type {
  RecognitionMultiIntegerRequest,
  RecognitionMultiIntegerProposalSet,
} from "../model/recognitionMultiIntegerProposals";

export async function previewRecognitionMultiInteger(request: RecognitionMultiIntegerRequest): Promise<RecognitionMultiIntegerProposalSet> {
  const response = await httpClient.post<RecognitionMultiIntegerProposalSet>(
    "/api/catalog/recognition/training/multi-integer-proposals",
    request,
  );
  return response.data;
}