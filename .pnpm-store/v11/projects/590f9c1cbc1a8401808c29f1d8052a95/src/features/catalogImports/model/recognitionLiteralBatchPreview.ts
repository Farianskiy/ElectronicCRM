export interface RecognitionLiteralRowPreviewResult {
  rowId: string;
  rowNumber: number;
  productName: string | null;
  status: string;
  currentValue: string | null;
  proposedValue: string | null;
  matchPositions: number[];
}

export interface RecognitionLiteralBatchPreviewPage {
  draftId: string;
  batchId: string;
  checkedAtUtc: string;
  page: number;
  pageSize: number;
  hasMore: boolean;
  items: RecognitionLiteralRowPreviewResult[];
}