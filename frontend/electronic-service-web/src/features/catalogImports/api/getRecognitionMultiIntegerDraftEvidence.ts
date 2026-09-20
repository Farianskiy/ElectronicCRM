import { httpClient } from "@/shared/api/httpClient";

export interface RecognitionMultiIntegerDraftEvidenceItem {
  exampleId: string;
  characteristicDefinitionId: string;
  productName: string;
  rawValue: string;
  normalizedValue: string;
  spanStart: number;
  spanLength: number;
  isSupporting: boolean;
  confirmedAtUtc: string;
  revokedAtUtc: string | null;
}

export interface RecognitionMultiIntegerDraftEvidencePage {
  draftId: string;
  items: RecognitionMultiIntegerDraftEvidenceItem[];
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export async function getRecognitionMultiIntegerDraftEvidence(
  draftId: string,
  page: number,
  signal?: AbortSignal,
): Promise<RecognitionMultiIntegerDraftEvidencePage> {
  const response =
    await httpClient.get<RecognitionMultiIntegerDraftEvidencePage>(
      `/api/catalog/recognition/training/multi-integer-drafts/${encodeURIComponent(draftId)}/evidence`,
      {
        params: { page, pageSize: 20 },
        signal,
      },
    );

  return response.data;
}