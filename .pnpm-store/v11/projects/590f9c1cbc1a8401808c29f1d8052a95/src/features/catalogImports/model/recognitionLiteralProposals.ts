export interface RecognitionTrainingScope {
  manufacturerId: string;
  productTypeId: string;
  characteristicDefinitionId: string;
}

export interface RecognitionLiteralProposal {
  literal: string;
  normalizedValue: string;
  matchedNameCount: number;
  supportingExampleIds: string[];
  conflictingExampleIds: string[];
  passedExamples: boolean;
}

export interface RecognitionTrainingIssue {
  code: string;
  message: string;
  exampleIds: string[];
}

export interface RecognitionLiteralProposalSet {
  scope: RecognitionTrainingScope;
  generatorVersion: string;
  proposals: RecognitionLiteralProposal[];
  issues: RecognitionTrainingIssue[];
}