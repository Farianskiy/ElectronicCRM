import { httpClient } from "@/shared/api/httpClient";
import type { RecognitionTrainingScope } from "../model/recognitionLiteralProposals";

export const recognitionIntegerDraftsQueryRoot = ["recognition-integer-drafts"] as const;

export interface RecognitionIntegerDraftListItem {
  id: string;
  prefix: string;
  suffixes: string[];
  generatorVersion: string;
  matchedNameCount: number;
  distinctValueCount: number;
  checkedExampleCount: number;
  supportingExampleCount: number;
  createdAtUtc: string;
}

export interface RecognitionIntegerDraftPage {
  items: RecognitionIntegerDraftListItem[];
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export async function getRecognitionIntegerDrafts(scope: RecognitionTrainingScope, page: number): Promise<RecognitionIntegerDraftPage> {
  const response = await httpClient.get<RecognitionIntegerDraftPage>("/api/catalog/recognition/training/integer-drafts", {
    params: { ...scope, page, pageSize: 10 },
  });
  return response.data;
}