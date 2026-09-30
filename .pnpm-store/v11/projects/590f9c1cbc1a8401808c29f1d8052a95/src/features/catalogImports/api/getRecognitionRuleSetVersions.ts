import { httpClient } from "@/shared/api/httpClient";

export const recognitionRuleSetVersionsQueryRoot = [
  "recognition-rule-set-versions",
] as const;

export interface RecognitionRuleSetVersionListItem {
  id: string;
  versionNumber: number;
  name: string;
  entryCount: number;
  createdAtUtc: string;
}

export interface RecognitionRuleSetVersionPage {
  items: RecognitionRuleSetVersionListItem[];
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export interface RecognitionRuleSetVersionEntry {
  position: number;
  kind: number;
  draftId: string;
}

export interface RecognitionRuleSetVersionDetails {
  id: string;
  manufacturerId: string;
  productTypeId: string;
  versionNumber: number;
  name: string;
  createdAtUtc: string;
  entries: RecognitionRuleSetVersionEntry[];
}

export async function getRecognitionRuleSetVersions(
  manufacturerId: string,
  productTypeId: string,
  page: number,
  signal?: AbortSignal,
): Promise<RecognitionRuleSetVersionPage> {
  const response = await httpClient.get<RecognitionRuleSetVersionPage>(
    "/api/catalog/recognition/training/rule-set-versions",
    {
      params: { manufacturerId, productTypeId, page, pageSize: 10 },
      signal,
    },
  );

  return response.data;
}

export async function getRecognitionRuleSetVersion(
  versionId: string,
  signal?: AbortSignal,
): Promise<RecognitionRuleSetVersionDetails> {
  const response = await httpClient.get<RecognitionRuleSetVersionDetails>(
    `/api/catalog/recognition/training/rule-set-versions/${encodeURIComponent(versionId)}`,
    { signal },
  );

  return response.data;
}