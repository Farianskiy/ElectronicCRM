import { httpClient } from "@/shared/api/httpClient";
import type {
  CreateCatalogPriceCalculationRequest,
  CreateCatalogPriceCalculationResponse,
} from "../model/types";

export async function createCatalogPriceCalculation(
  request: CreateCatalogPriceCalculationRequest,
): Promise<CreateCatalogPriceCalculationResponse> {
  const response =
    await httpClient.post<CreateCatalogPriceCalculationResponse>(
      "/api/catalog/price-calculations",
      request,
    );

  return response.data;
}