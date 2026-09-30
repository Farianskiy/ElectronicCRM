import { httpClient } from "@/shared/api/httpClient";
import type {
  CatalogCharacteristicDefinition,
} from "../model/types";

export async function getCatalogCharacteristicDefinitions(
  search?: string,
): Promise<CatalogCharacteristicDefinition[]> {
  const response = await httpClient.get<
    CatalogCharacteristicDefinition[]
  >("/api/catalog/characteristic-definitions", {
    params: {
      search:
        search?.trim().length
          ? search.trim()
          : undefined,
    },
  });

  return response.data;
}