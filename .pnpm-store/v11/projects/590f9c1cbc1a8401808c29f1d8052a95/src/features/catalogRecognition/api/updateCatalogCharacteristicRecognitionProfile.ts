import { httpClient } from "@/shared/api/httpClient";
import type { UpdateCatalogCharacteristicRecognitionProfileParameters } from "../model/types";

export async function updateCatalogCharacteristicRecognitionProfile(
  parameters: UpdateCatalogCharacteristicRecognitionProfileParameters,
): Promise<void> {
  await httpClient.put(
    `/api/catalog/recognition/profiles/${encodeURIComponent(parameters.profileId)}`,
    {
      strategyKind: parameters.strategyKind,
      priority: parameters.priority,
      minimumConfidence: parameters.minimumConfidence,
      configurationJson: parameters.configurationJson,
    },
  );
}