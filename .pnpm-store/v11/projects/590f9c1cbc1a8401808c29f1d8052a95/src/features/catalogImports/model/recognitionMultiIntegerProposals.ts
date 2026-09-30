import type { RecognitionTrainingIssue } from "./recognitionLiteralProposals";

export interface RecognitionMultiIntegerRequest {
  manufacturerId: string;
  productTypeId: string;
  characteristicDefinitionIds: string[];
  productNames: string[];
}

export interface RecognitionMultiIntegerPart {
  literal: string | null;
  characteristicDefinitionId: string | null;
}

export interface RecognitionMultiIntegerProposal {
  pattern: {
    parts: RecognitionMultiIntegerPart[];
  };
  matchedNameCount: number;
  supportingNameCount: number;
  fields: {
    characteristicDefinitionId: string;
    distinctValueCount: number;
  }[];
  supportingExampleIds: string[];
  conflictingExampleIds: string[];
  passedExamples: boolean;
}

export interface RecognitionMultiIntegerProposalSet {
  manufacturerId: string;
  productTypeId: string;
  generatorVersion: string;
  proposals: RecognitionMultiIntegerProposal[];
  issues: RecognitionTrainingIssue[];
}