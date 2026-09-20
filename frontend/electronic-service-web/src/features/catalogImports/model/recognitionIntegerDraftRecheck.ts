import type { RecognitionIntegerPatternProposal } from "./recognitionIntegerPatterns";
import type { RecognitionTrainingIssue } from "./recognitionLiteralProposals";

export interface RecognitionIntegerDraftRecheckResult {
  draftId: string;
  checkedAtUtc: string;
  currentGeneratorVersion: string;
  generatorVersionMatches: boolean;
  selectionComplete: boolean;
  evidenceUnchanged: boolean;
  loadedExampleCount: number;
  addedExampleIds: string[];
  missingExampleIds: string[];
  evaluation: RecognitionIntegerPatternProposal | null;
  issues: RecognitionTrainingIssue[];
  passedCurrentExamples: boolean;
}