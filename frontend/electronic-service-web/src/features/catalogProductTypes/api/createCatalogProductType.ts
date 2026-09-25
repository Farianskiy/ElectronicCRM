import { httpClient } from "@/shared/api/httpClient";
import type {
  CreateCatalogProductTypeRequest,
  CreateCatalogProductTypeResponse,
} from "@/features/catalogMetadata/model/types";

export async function createCatalogProductType(
  request: CreateCatalogProductTypeRequest,
): Promise<CreateCatalogProductTypeResponse> {
  const response = await httpClient.post<CreateCatalogProductTypeResponse>(
    "/api/catalog/product-types",
    request,
  );

  return response.data;
}
