import { httpClient } from "@/shared/api/httpClient";

export interface RecognitionIntegerDraftEvidenceItem {
  exampleId: string;
  productName: string;
  rawValue: string;
  normalizedValue: string;
  spanStart: number;
  spanLength: number;
  isSupporting: boolean;
  confirmedAtUtc: string;
  revokedAtUtc: string | null;
}

export interface RecognitionIntegerDraftEvidencePage {
  draftId: string;
  items: RecognitionIntegerDraftEvidenceItem[];
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export async function getRecognitionIntegerDraftEvidence(draftId: string, page: number): Promise<RecognitionIntegerDraftEvidencePage> {
  const response = await httpClient.get<RecognitionIntegerDraftEvidencePage>(
    `/api/catalog/recognition/training/integer-drafts/${draftId}/evidence`,
    { params: { page, pageSize: 20 } },
  );
  return response.data;
}