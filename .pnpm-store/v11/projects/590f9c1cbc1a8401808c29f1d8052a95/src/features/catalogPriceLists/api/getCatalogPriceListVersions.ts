import { httpClient } from "@/shared/api/httpClient";
import type {
  GetCatalogPriceListVersionsParams,
  GetCatalogPriceListVersionsResponse,
} from "../model/types";

export async function getCatalogPriceListVersions(
  params: GetCatalogPriceListVersionsParams,
): Promise<GetCatalogPriceListVersionsResponse> {
  const queryParams = new URLSearchParams();

  queryParams.set("manufacturerId", params.manufacturerId);

  if (params.status) {
    queryParams.set("status", params.status);
  }

  queryParams.set("page", params.page.toString());
  queryParams.set("pageSize", params.pageSize.toString());

  const response =
    await httpClient.get<GetCatalogPriceListVersionsResponse>(
      `/api/catalog/price-lists?${queryParams.toString()}`,
    );

  return response.data;
}