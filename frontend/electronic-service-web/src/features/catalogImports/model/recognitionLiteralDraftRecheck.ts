import type { RecognitionLiteralProposal, RecognitionTrainingIssue } from "./recognitionLiteralProposals";

export interface RecognitionLiteralDraftRecheckResult {
  draftId: string;
  checkedAtUtc: string;
  currentGeneratorVersion: string;
  generatorVersionMatches: boolean;
  selectionComplete: boolean;
  evidenceUnchanged: boolean;
  currentExampleCount: number;
  addedExampleIds: string[];
  missingExampleIds: string[];
  proposal: RecognitionLiteralProposal | null;
  issues: RecognitionTrainingIssue[];
  passedCurrentExamples: boolean;
}