import { httpClient } from "@/shared/api/httpClient";
import type { SetCatalogDictionaryTermActiveParameters } from "../model/types";

export async function setCatalogDictionaryTermActive(
  parameters: SetCatalogDictionaryTermActiveParameters,
): Promise<void> {
  await httpClient.put(
    `/api/catalog/dictionary/terms/${parameters.termId}/active`,
    {
      isActive: parameters.isActive,
      reason: parameters.reason,
    },
  );
}