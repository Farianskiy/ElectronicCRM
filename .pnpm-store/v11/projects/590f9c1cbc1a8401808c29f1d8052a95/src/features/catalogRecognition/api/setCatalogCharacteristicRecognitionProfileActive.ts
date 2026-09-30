import { httpClient } from "@/shared/api/httpClient";
import type { SetCatalogCharacteristicRecognitionProfileActiveParameters } from "../model/types";

export async function setCatalogCharacteristicRecognitionProfileActive(
  parameters: SetCatalogCharacteristicRecognitionProfileActiveParameters,
): Promise<void> {
  await httpClient.put(
    `/api/catalog/recognition/profiles/${encodeURIComponent(parameters.profileId)}/active`,
    {
      isActive: parameters.isActive,
    },
  );
}