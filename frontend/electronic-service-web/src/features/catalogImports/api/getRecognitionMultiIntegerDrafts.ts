import { httpClient } from "@/shared/api/httpClient";

export const recognitionMultiIntegerDraftsQueryRoot = [
  "recognition-multi-integer-drafts",
] as const;

export interface RecognitionMultiIntegerDraftPartItem {
  position: number;
  literal: string | null;
  characteristicDefinitionId: string | null;
  distinctValueCount: number;
}

export interface RecognitionMultiIntegerDraftListItem {
  id: string;
  manufacturerId: string;
  productTypeId: string;
  generatorVersion: string;
  parts: RecognitionMultiIntegerDraftPartItem[];
  matchedNameCount: number;
  supportingNameCount: number;
  checkedExampleCount: number;
  supportingExampleCount: number;
  createdAtUtc: string;
}

export interface RecognitionMultiIntegerDraftPage {
  items: RecognitionMultiIntegerDraftListItem[];
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export async function getRecognitionMultiIntegerDrafts(
  manufacturerId: string,
  productTypeId: string,
  page: number,
): Promise<RecognitionMultiIntegerDraftPage> {
  const response = await httpClient.get<RecognitionMultiIntegerDraftPage>(
    "/api/catalog/recognition/training/multi-integer-drafts",
    {
      params: {
        manufacturerId,
        productTypeId,
        page,
        pageSize: 10,
      },
    },
  );

  return response.data;
}