import { httpClient } from "@/shared/api/httpClient";

export interface UpdateCatalogProductPriceRequest {
  amount: number;
  currency: string;
}

export async function updateCatalogProductPrice(
  productId: string,
  request: UpdateCatalogProductPriceRequest,
): Promise<void> {
  const encodedProductId = encodeURIComponent(productId);

  await httpClient.post(
    `/api/catalog/products/${encodedProductId}/price`,
    request,
  );
}