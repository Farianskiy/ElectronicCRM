import { httpClient } from "@/shared/api/httpClient";
import type {
  CreateCatalogCharacteristicRecognitionProfileParameters,
  CreateCatalogCharacteristicRecognitionProfileResult,
} from "../model/types";

export async function createCatalogCharacteristicRecognitionProfile(
  parameters: CreateCatalogCharacteristicRecognitionProfileParameters,
): Promise<CreateCatalogCharacteristicRecognitionProfileResult> {
  const response =
    await httpClient.post<CreateCatalogCharacteristicRecognitionProfileResult>(
      "/api/catalog/recognition/profiles",
      {
        productTypeCode: parameters.productTypeCode,
        characteristicDefinitionId:
          parameters.characteristicDefinitionId,
        strategyKind: parameters.strategyKind,
        priority: parameters.priority,
        minimumConfidence: parameters.minimumConfidence,
        configurationJson: parameters.configurationJson,
      },
    );

  return response.data;
}