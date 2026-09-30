import { httpClient } from "@/shared/api/httpClient";
import type { RecognitionRuleSetBatchPreviewItem } from "./previewRecognitionRuleSetBatch";

const root = "/api/catalog/recognition/training";

export const recognitionRuleSetStateQueryRoot = [
  "recognition-rule-set-state",
] as const;

export interface RecognitionRuleSetState {
  manufacturerId: string;
  productTypeId: string;
  sequenceNumber: number;
  switchId: string | null;
  activeVersionId: string | null;
  reportId: string | null;
  changedAtUtc: string | null;
}

export interface RecognitionRuleSetReport {
  id: string;
  ruleSetVersionId: string;
  batchId: string;
  completedAtUtc: string;
  totalRowsCount: number;
  proposedRowsCount: number;
  conflictRowsCount: number;
  noMatchRowsCount: number;
  outsideScopeRowsCount: number;
}

export interface RecognitionRuleSetReportPage {
  summary: RecognitionRuleSetReport;
  startedAtUtc: string;
  batchVersion: number;
  evaluatorVersion: string;
  snapshotFormatVersion: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
  items: RecognitionRuleSetBatchPreviewItem[];
}

export async function getRecognitionRuleSetState(
  manufacturerId: string,
  productTypeId: string,
  signal?: AbortSignal,
): Promise<RecognitionRuleSetState> {
  const response = await httpClient.get<RecognitionRuleSetState>(
    `${root}/rule-set-state`,
    { params: { manufacturerId, productTypeId }, signal },
  );
  return response.data;
}

export async function createRecognitionRuleSetReport(
  versionId: string,
  batchId: string,
): Promise<RecognitionRuleSetReport> {
  const response = await httpClient.post<RecognitionRuleSetReport>(
    `${root}/rule-set-versions/${encodeURIComponent(versionId)}/reports/batches/${encodeURIComponent(batchId)}`,
  );
  return response.data;
}

export async function getRecognitionRuleSetReport(
  reportId: string,
  page: number,
  signal?: AbortSignal,
): Promise<RecognitionRuleSetReportPage> {
  const response = await httpClient.get<RecognitionRuleSetReportPage>(
    `${root}/rule-set-reports/${encodeURIComponent(reportId)}`,
    { params: { page }, signal },
  );
  return response.data;
}

export async function switchRecognitionRuleSet(request: {
  manufacturerId: string;
  productTypeId: string;
  expectedSequenceNumber: number;
  newVersionId: string | null;
  reportId: string | null;
  reason: string;
  confirmed: boolean;
}): Promise<void> {
  await httpClient.post(`${root}/rule-set-switches`, request);
}

export const recognitionRuleSetReportsQueryRoot = [
  "recognition-rule-set-reports",
] as const;

export async function getRecentRecognitionRuleSetReports(
  versionId: string,
  batchId: string,
  signal?: AbortSignal,
): Promise<RecognitionRuleSetReport[]> {
  const response = await httpClient.get<RecognitionRuleSetReport[]>(
    `${root}/rule-set-reports/recent`,
    { params: { versionId, batchId }, signal },
  );

  return response.data;
}

export interface RecognitionRuleSetTrainingCheck {
  versionId: string;
  checkedAtUtc: string;
  passedTrainingChecks: boolean;
  items: {
    draftId: string;
    ruleKind: string;
    passed: boolean;
    message: string;
  }[];
}

export async function checkRecognitionRuleSetTraining(
  versionId: string,
  signal?: AbortSignal,
): Promise<RecognitionRuleSetTrainingCheck> {
  const response = await httpClient.get<RecognitionRuleSetTrainingCheck>(
    `${root}/rule-set-versions/${encodeURIComponent(versionId)}/training-check`,
    { signal },
  );

  return response.data;
}