import { httpClient } from "@/shared/api/httpClient";
import type { CatalogDictionaryTerm } from "../model/types";

export const catalogDictionaryTermsQueryKey = [
  "catalog-dictionary-terms",
] as const;

export async function getCatalogDictionaryTerms(): Promise<
  CatalogDictionaryTerm[]
> {
  const response = await httpClient.get<CatalogDictionaryTerm[]>(
    "/api/catalog/dictionary/terms",
  );

  return response.data;
}