import { httpClient } from "@/shared/api/httpClient";

export interface RecognitionRuleSetNamePreviewRequest {
  manufacturerId: string;
  productTypeId: string;
  productName: string;
}

export interface RecognitionRuleSetNamePreviewSource {
  draftId: string;
  characteristicDefinitionId: string;
  normalizedValue: string;
  rawValue: string;
  spanStart: number;
  spanLength: number;
}

export interface RecognitionRuleSetNamePreviewCharacteristic {
  characteristicDefinitionId: string;
  proposedValue: string | null;
  hasConflict: boolean;
  alternativeValues: string[];
  sources: RecognitionRuleSetNamePreviewSource[];
}

export interface RecognitionRuleSetCharacteristicDescription {
  characteristicDefinitionId: string;
  name: string;
  unit: string | null;
}

export interface RecognitionRuleSetNamePreviewResult {
  versionId: string;
  versionNumber: number;
  manufacturerId: string;
  productTypeId: string;
  productName: string;
  checkedAtUtc: string;
  characteristicDefinitions?: RecognitionRuleSetCharacteristicDescription[];
  resolution: {
    hasConflicts: boolean;
    characteristics: RecognitionRuleSetNamePreviewCharacteristic[];
  };
}

export async function previewRecognitionRuleSetName(
  versionId: string,
  request: RecognitionRuleSetNamePreviewRequest,
): Promise<RecognitionRuleSetNamePreviewResult> {
  const response =
    await httpClient.post<RecognitionRuleSetNamePreviewResult>(
      `/api/catalog/recognition/training/rule-set-versions/${encodeURIComponent(versionId)}/preview-name`,
      request,
    );

  return response.data;
}