import { httpClient } from "@/shared/api/httpClient";

export interface EvaluationCounts { total: number; correct: number; incorrect: number; missing: number; conflicts: number; correctRate: number | null }
export interface EvaluationMetrics { names: number; current: EvaluationCounts; candidate: EvaluationCounts; improvements: number; regressions: number }
export interface EvaluationPage {
  reportId: string; manufacturerId: string; productTypeId: string; currentVersionId: string | null;
  manufacturerName: string | null; productTypeName: string | null;
  decisionSummary?: string; usedForApproval?: boolean;
  candidateVersionId: string; sequenceNumber: number; historicalReplay: boolean;
  policy: { version: string; minimumControlNames: number; minimumControlUnits: number; minimumImprovements: number } | null;
  readiness: { state: string; reasons: { code: string; message: string }[] };
  control: EvaluationMetrics | null; trainingDiagnostic: EvaluationMetrics | null;
  characteristics: { characteristicId: string; metrics: EvaluationMetrics }[];
  inputExamples: number; duplicateExamples: number; labelConflicts: number; overlapUnits: number;
  trainingPassed: boolean | null; page: number; total: number;
  items: { productName: string; characteristicCode: string; expectedValues: string[];
    sources: { exampleId: string; feedbackId: string }[]; trainingOverlap: boolean; labelConflict: boolean;
    current: { status: string; value: string | null }; candidate: { status: string; value: string | null }; change: string }[];
}
const root = "/api/catalog/recognition/training/rule-set-reports";
export async function getEvaluationReport(id: string, page = 1, replay = false, kind: "rules" | "dictionary" = "rules"): Promise<EvaluationPage> {
  if (kind === "dictionary") {
    const url = `/api/catalog/assistant/dictionary-suggestions/evaluations/${id}`;
    type Response = { evaluation: EvaluationPage; decision: { phrase: string; targetCode: string; targetValue: string; priority: number }; usedForApproval: boolean };
    const { data } = replay ? await httpClient.post<Response>(`${url}/replay`, null, { params: { page } }) : await httpClient.get<Response>(url, { params: { page } });
    return { ...data.evaluation, decisionSummary: `${data.decision.phrase} → ${data.decision.targetCode} = ${data.decision.targetValue}; приоритет ${data.decision.priority}`, usedForApproval: data.usedForApproval };
  }
  return replay
    ? (await httpClient.post<EvaluationPage>(`${root}/${id}/evaluation/replay`, null, { params: { page } })).data
    : (await httpClient.get<EvaluationPage>(`${root}/${id}/evaluation`, { params: { page } })).data;
}
