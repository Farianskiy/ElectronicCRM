import { httpClient } from "@/shared/api/httpClient";

export async function deleteCharacteristicDefinition(characteristicDefinitionId: string): Promise<void> {
  const encodedCharacteristicDefinitionId = encodeURIComponent(characteristicDefinitionId);

  await httpClient.delete(`/api/catalog/characteristic-definitions/${encodedCharacteristicDefinitionId}`);
}