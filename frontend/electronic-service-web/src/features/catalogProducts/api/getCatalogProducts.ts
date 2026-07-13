import { httpClient } from "@/shared/api/httpClient";
import type {
  CatalogProductsResponse,
  CatalogProductsSearchParams,
} from "../model/types";

export async function getCatalogProducts(
  params: CatalogProductsSearchParams,
): Promise<CatalogProductsResponse> {
  const queryParams = new URLSearchParams();

  if (params.searchText.trim().length > 0) {
    queryParams.set("searchText", params.searchText.trim());
  }

  queryParams.set("onlyInStock", params.onlyInStock.toString());
  queryParams.set("page", params.page.toString());
  queryParams.set("pageSize", params.pageSize.toString());

  const response = await httpClient.get<CatalogProductsResponse>(
    `/api/catalog/products?${queryParams.toString()}`,
  );

  return response.data;
}