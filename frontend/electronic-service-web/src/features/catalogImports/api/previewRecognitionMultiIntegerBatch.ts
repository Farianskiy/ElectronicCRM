import { httpClient } from "@/shared/api/httpClient";
import type {
  RecognitionMultiIntegerRequest,
  RecognitionMultiIntegerProposal,
} from "../model/recognitionMultiIntegerProposals";

export interface RecognitionMultiIntegerBatchRequest extends RecognitionMultiIntegerRequest {
  batchId: string;
  generatorVersion: string;
  pattern: RecognitionMultiIntegerProposal["pattern"];
  page: number;
  pageSize: number;
}

export interface RecognitionMultiIntegerBatchPage {
  batchId: string;
  generatorVersion: string;
  pattern: RecognitionMultiIntegerProposal["pattern"];
  checkedAtUtc: string;
  page: number;
  pageSize: number;
  hasMore: boolean;
  items: {
    matchesSelectedTrainingName: boolean;
    result: {
      rowId: string;
      rowNumber: number;
      productName: string | null;
      status: string;
      fields: {
        characteristicDefinitionId: string;
        status: string;
        currentValue: string | null;
        proposedValue: string | null;
        capture: {
          characteristicDefinitionId: string;
          rawValue: string;
          normalizedValue: string;
          spanStart: number;
          spanLength: number;
        } | null;
      }[];
    };
  }[];
}

export async function previewRecognitionMultiIntegerBatch(request: RecognitionMultiIntegerBatchRequest): Promise<RecognitionMultiIntegerBatchPage> {
  const response = await httpClient.post<RecognitionMultiIntegerBatchPage>(
    "/api/catalog/recognition/training/multi-integer-proposals/batch-preview",
    request,
  );
  return response.data;
}