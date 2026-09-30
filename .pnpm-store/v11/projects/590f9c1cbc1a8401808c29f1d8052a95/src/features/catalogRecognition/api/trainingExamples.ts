import { httpClient } from "@/shared/api/httpClient";
import { downloadFileResponse } from "@/shared/api/downloadFileResponse";

export interface TrainingExample {
  id: string;
  sourceFeedbackId: string;
  isEvaluationOnly: boolean;
  sourceAvailable: boolean;
  sourceExcludedAtUtc: string | null;
  sourceExclusionReason: string | null;
  productName: string;
  rawValue: string;
  normalizedValue: string;
  manufacturerName: string;
  productTypeName: string;
  characteristicName: string;
  confirmedAtUtc: string;
  revokedAtUtc: string | null;
  revocationReason: string | null;
  importBatchId: string | null;
}

export interface TrainingExampleFilter {
  manufacturerId?: string;
  productTypeId?: string;
  characteristicDefinitionId?: string;
  status: string;
  page: number;
}

const endpoint = "/api/catalog/recognition/training-examples";

export async function getTrainingExample(id: string) {
  return (await httpClient.get<TrainingExample>(`${endpoint}/${id}`)).data;
}

export async function getTrainingExamples(params: TrainingExampleFilter) {
  return (await httpClient.get<{ items: TrainingExample[]; page: number; hasMore: boolean }>(endpoint, { params })).data;
}

export async function setTrainingExamplePurpose(id: string, evaluationOnly: boolean) {
  await httpClient.post(`${endpoint}/${id}/purpose`, { evaluationOnly });
}

export async function revokeTrainingExample(id: string, reason: string) {
  await httpClient.post(`${endpoint}/${id}/revoke`, { reason });
}

export async function exportConfirmedExamples(params: TrainingExampleFilter) {
  const response = await httpClient.get<Blob>(`${endpoint}/export`, { params, responseType: "blob" });
  downloadFileResponse(response, "confirmed-examples-v1.jsonl");
}
