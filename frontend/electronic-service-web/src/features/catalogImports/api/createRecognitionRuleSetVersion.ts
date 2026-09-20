import { httpClient } from "@/shared/api/httpClient";

export interface CreateRecognitionRuleSetVersionRequest {
  manufacturerId: string;
  productTypeId: string;
  name: string;
  entries: {
    kind: 1 | 2 | 3;
    draftId: string;
  }[];
}

export interface CreateRecognitionRuleSetVersionResponse {
  id: string;
  versionNumber: number;
}

export async function createRecognitionRuleSetVersion(
  request: CreateRecognitionRuleSetVersionRequest,
): Promise<CreateRecognitionRuleSetVersionResponse> {
  const response =
    await httpClient.post<CreateRecognitionRuleSetVersionResponse>(
      "/api/catalog/recognition/training/rule-set-versions",
      request,
    );

  return response.data;
}