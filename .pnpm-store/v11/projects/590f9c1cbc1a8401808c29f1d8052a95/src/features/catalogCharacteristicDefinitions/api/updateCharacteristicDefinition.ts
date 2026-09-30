import { httpClient } from "@/shared/api/httpClient";
import type {
  UpdateCharacteristicDefinitionRequest,
} from "../model/types";

export async function updateCharacteristicDefinition(
  characteristicDefinitionId: string,
  request: UpdateCharacteristicDefinitionRequest,
): Promise<void> {
  const encodedDefinitionId =
    encodeURIComponent(characteristicDefinitionId);

  await httpClient.put(
    `/api/catalog/characteristic-definitions/${encodedDefinitionId}`,
    request,
  );
}