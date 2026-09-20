import type { RecognitionTrainingScope, RecognitionTrainingIssue } from "./recognitionLiteralProposals";
import type { RecognitionIntegerPattern } from "./recognitionIntegerPatterns";

export interface RecognitionIntegerBatchPreviewRequest {
  batchId: string;
  scope: RecognitionTrainingScope;
  generatorVersion: string;
  pattern: RecognitionIntegerPattern;
  page: number;
  pageSize: number;
}

export interface RecognitionIntegerCapture {
  rawValue: string;
  normalizedValue: string;
  spanStart: number;
  spanLength: number;
}

export interface RecognitionIntegerBatchPreviewItem {
  result: {
    rowId: string;
    rowNumber: number;
    productName: string | null;
    status: string;
    currentValue: string | null;
    proposedValue: string | null;
    captures: RecognitionIntegerCapture[];
  };
  matchesTrainingName: boolean;
}

export interface RecognitionIntegerBatchPreviewPage {
  batchId: string;
  scope: RecognitionTrainingScope;
  generatorVersion: string;
  pattern: RecognitionIntegerPattern;
  checkedAtUtc: string;
  page: number;
  pageSize: number;
  hasMore: boolean;
  items: RecognitionIntegerBatchPreviewItem[];
  diagnostics: RecognitionTrainingIssue[];
}