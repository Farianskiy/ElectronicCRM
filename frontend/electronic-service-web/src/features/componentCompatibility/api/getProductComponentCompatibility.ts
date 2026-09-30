import { httpClient } from "@/shared/api/httpClient";
import type { ProductComponentCompatibility } from "../model/types";

export async function getProductComponentCompatibility(
  productId: string,
): Promise<ProductComponentCompatibility> {
  const response = await httpClient.get<ProductComponentCompatibility>(
    `/api/catalog/component-compatibility/products/${encodeURIComponent(productId)}`,
  );

  return response.data;
}
