import { httpClient } from "@/shared/api/httpClient";
import type {
  AddCatalogDictionaryTermParameters,
  AddCatalogDictionaryTermResult,
} from "../model/types";

export async function addCatalogDictionaryTerm(
  parameters: AddCatalogDictionaryTermParameters,
): Promise<AddCatalogDictionaryTermResult> {
  const response = await httpClient.post<AddCatalogDictionaryTermResult>(
    "/api/catalog/dictionary/terms",
    {
      productTypeCode: parameters.productTypeCode,
      phrase: parameters.phrase,
      kind: parameters.kind,
      targetCode: parameters.targetCode,
      targetValue: parameters.targetValue,
      priority: parameters.priority,
    },
  );

  return response.data;
}