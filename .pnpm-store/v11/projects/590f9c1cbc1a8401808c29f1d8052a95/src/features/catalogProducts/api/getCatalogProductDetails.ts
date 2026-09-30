import { httpClient } from "@/shared/api/httpClient";
import type { CatalogProductDetails } from "../model/types";

export async function getCatalogProductDetails(
  productId: string,
): Promise<CatalogProductDetails> {
  const response = await httpClient.get<CatalogProductDetails>(
    `/api/catalog/products/${productId}`,
  );

  return response.data;
}