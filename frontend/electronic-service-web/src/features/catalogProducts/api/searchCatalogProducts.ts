import { httpClient } from "@/shared/api/httpClient";
import type {
  AdvancedCatalogProductsSearchParams,
  CatalogProductsResponse,
} from "../model/types";

export async function searchCatalogProducts(
  params: AdvancedCatalogProductsSearchParams,
): Promise<CatalogProductsResponse> {
  const response = await httpClient.post<CatalogProductsResponse>(
    "/api/catalog/products/search",
    {
      search: params.search,
      productTypeCode: params.productTypeCode,
      manufacturer: params.manufacturer,
      characteristics: params.characteristics ?? [],
      page: params.page,
      pageSize: params.pageSize,
    },
  );

  return response.data;
}