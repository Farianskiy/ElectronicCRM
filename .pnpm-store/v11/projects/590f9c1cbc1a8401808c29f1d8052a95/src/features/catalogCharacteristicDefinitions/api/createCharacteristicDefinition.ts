import { httpClient } from "@/shared/api/httpClient";
import type {
  CreateCharacteristicDefinitionRequest,
  CreateCharacteristicDefinitionResponse,
} from "../model/types";

export async function createCharacteristicDefinition(
  request: CreateCharacteristicDefinitionRequest,
): Promise<CreateCharacteristicDefinitionResponse> {
  const response =
    await httpClient.post<
      CreateCharacteristicDefinitionResponse
    >(
      "/api/catalog/characteristic-definitions",
      request,
    );

  return response.data;
}