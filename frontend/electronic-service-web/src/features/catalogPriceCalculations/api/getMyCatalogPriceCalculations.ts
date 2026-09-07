import { httpClient } from "@/shared/api/httpClient";
import type {
  GetMyCatalogPriceCalculationsParams,
  GetMyCatalogPriceCalculationsResponse,
} from "../model/types";

export async function getMyCatalogPriceCalculations(
  params: GetMyCatalogPriceCalculationsParams,
): Promise<GetMyCatalogPriceCalculationsResponse> {
  const queryParams = new URLSearchParams();

  if (params.status) {
    queryParams.set("status", params.status);
  }

  queryParams.set("page", params.page.toString());
  queryParams.set("pageSize", params.pageSize.toString());

  const response =
    await httpClient.get<GetMyCatalogPriceCalculationsResponse>(
      `/api/catalog/price-calculations?${queryParams.toString()}`,
    );

  return response.data;
}