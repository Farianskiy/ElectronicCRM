import { httpClient } from "@/shared/api/httpClient";
import type { CatalogProductTypeMetadata } from "../model/types";

export async function getCatalogProductTypes(): Promise<
  CatalogProductTypeMetadata[]
> {
  const response = await httpClient.get<CatalogProductTypeMetadata[]>(
    "/api/catalog/metadata/product-types",
  );

  return response.data;
}