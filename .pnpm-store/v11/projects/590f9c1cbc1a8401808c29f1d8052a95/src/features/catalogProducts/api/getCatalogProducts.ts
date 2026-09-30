import { httpClient } from "@/shared/api/httpClient";
import type {
  CatalogProductsResponse,
  CatalogProductsSearchParams,
} from "../model/types";

export async function getCatalogProducts(
  params: CatalogProductsSearchParams,
): Promise<CatalogProductsResponse> {
  const queryParams = new URLSearchParams();

  if (params.search.trim().length > 0) {
    queryParams.set("search", params.search.trim());
  }

  if (params.productTypeCode && params.productTypeCode.trim().length > 0) {
    queryParams.set("productTypeCode", params.productTypeCode.trim());
  }

  if (params.manufacturer && params.manufacturer.trim().length > 0) {
    queryParams.set("manufacturer", params.manufacturer.trim());
  }

  queryParams.set("page", params.page.toString());
  queryParams.set("pageSize", params.pageSize.toString());

  const response = await httpClient.get<CatalogProductsResponse>(
    `/api/catalog/products?${queryParams.toString()}`,
  );

  return response.data;
}