import { httpClient } from "@/shared/api/httpClient";
import type { CatalogManufacturerMetadata } from "../model/types";

export async function getCatalogManufacturers(): Promise<
  CatalogManufacturerMetadata[]
> {
  const response = await httpClient.get<CatalogManufacturerMetadata[]>(
    "/api/catalog/metadata/manufacturers",
  );

  return response.data;
}