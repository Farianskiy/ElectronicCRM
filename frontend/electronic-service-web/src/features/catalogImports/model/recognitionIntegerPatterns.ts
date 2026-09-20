import type { RecognitionTrainingScope, RecognitionTrainingIssue } from "./recognitionLiteralProposals";

export interface RecognitionIntegerPattern {
  prefix: string;
  suffixes: string[];
}

export interface RecognitionIntegerPatternProposal {
  pattern: RecognitionIntegerPattern;
  matchedNameCount: number;
  distinctValueCount: number;
  supportingExampleIds: string[];
  conflictingExampleIds: string[];
  passedExamples: boolean;
}

export interface RecognitionIntegerPatternProposalSet {
  scope: RecognitionTrainingScope;
  generatorVersion: string;
  proposals: RecognitionIntegerPatternProposal[];
  issues: RecognitionTrainingIssue[];
}