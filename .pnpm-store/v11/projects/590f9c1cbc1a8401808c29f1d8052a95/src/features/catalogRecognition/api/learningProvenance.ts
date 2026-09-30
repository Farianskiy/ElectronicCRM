import { httpClient } from "@/shared/api/httpClient";

type Page<T> = { items: T[]; total: number; page: number };
export interface LearningProvenance {
  importBatchId: string | null;
  feedbackId: string; canExclude: boolean; excludedAtUtc: string | null; exclusionReason: string | null;
  examples: Page<{ id: string; state: string }>;
  candidates: Page<{ id: string; suggestionId: string | null; evaluationReportId: string | null; dictionaryTermId: string | null; occurrenceCount: number; distinctProductCount: number; sufficientEvidence: boolean; needsReview: boolean; suggestionStatus: string | null }>;
  drafts: Page<{ id: string; exampleId: string; kind: string; supporting: boolean }>;
  versions: Page<{ id: string; draftId: string; kind: string; active: boolean; needsReview: boolean }>;
  switches: Page<{ id: string; previousVersionId: string | null; newVersionId: string | null; sequenceNumber: number }>;
}
const endpoint = "/api/catalog/recognition/learning-provenance";
export async function getLearningProvenance(id: string, page: number) {
  return (await httpClient.get<LearningProvenance>(`${endpoint}/examples/${id}`, { params: { page } })).data;
}
export async function excludeObservation(id: string, reason: string) {
  return (await httpClient.post<LearningProvenance>(`${endpoint}/feedback/${id}/exclude`, { reason })).data;
}
export async function getSuggestionEvidence(id: string) {
  return (await httpClient.get<{ evaluationReportId: string | null; revision: number; sufficientEvidence: boolean; occurrenceCount: number; distinctProductCount: number; needsReview: boolean }>(`${endpoint}/suggestions/${id}`)).data;
}
