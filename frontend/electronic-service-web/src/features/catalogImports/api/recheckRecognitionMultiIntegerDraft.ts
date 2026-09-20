import { httpClient } from "@/shared/api/httpClient";
import type { RecognitionTrainingIssue } from "../model/recognitionLiteralProposals";
import type { RecognitionMultiIntegerProposal } from "../model/recognitionMultiIntegerProposals";

export interface RecognitionMultiIntegerDraftRecheckResult {
  draftId: string;
  checkedAtUtc: string;
  currentGeneratorVersion: string;
  passed: boolean;
  selectionComplete: boolean;
  evidenceUnchanged: boolean | null;
  addedExampleIds: string[];
  missingExampleIds: string[];
  evaluation: RecognitionMultiIntegerProposal | null;
  issues: RecognitionTrainingIssue[];
}

export async function recheckRecognitionMultiIntegerDraft(
  draftId: string,
): Promise<RecognitionMultiIntegerDraftRecheckResult> {
  const response =
    await httpClient.get<RecognitionMultiIntegerDraftRecheckResult>(
      `/api/catalog/recognition/training/multi-integer-drafts/${encodeURIComponent(draftId)}/recheck`,
    );

  return response.data;
}