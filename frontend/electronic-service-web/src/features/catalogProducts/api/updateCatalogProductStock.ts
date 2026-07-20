import { httpClient } from "@/shared/api/httpClient";

export interface UpdateCatalogProductStockRequest {
  quantity: number;
}

export async function updateCatalogProductStock(
  productId: string,
  request: UpdateCatalogProductStockRequest,
): Promise<void> {
  const encodedProductId = encodeURIComponent(productId);

  await httpClient.post(
    `/api/catalog/products/${encodedProductId}/stock`,
    request,
  );
}